Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Module Program

    'Projet RetroYT 9.0 codé par Monokeros en 2026
    'Tous droits réservés. Licence freeware/open source.

    Public port As Integer = 80 'Port à écouter pour créer le serveur
    'Public patternpage As String = Nothing 'Page HTML modèle à renvoyer au client
    Public video_props As New Dictionary(Of String, VideoProperties)
    Public iso As Encoding = Encoding.GetEncoding("iso-8859-1")
    Public last_host As String = String.Empty
    Public link_color As String = "#800000"
    Public range_begin As Long = -1
    Public range_end As Long = -1
    Public number_of_vids As Integer = 0
    Public number_of_dls As Integer = 0
    Public number_of_channels As Integer = 0
    Public sw As New Stopwatch
    Public ip_list As New Dictionary(Of String, Decimal) 'Liste des adresses IP connectés, et du nombre de requêtes par IP.
    Public log_lock As New Object
    Public up_since As Date = Nothing
    'Public main_stream As String = String.Empty
    Public index_streams As New Dictionary(Of VideoCategories, String) 'Liste des vidéos contenues
    Public playlist_list As New Dictionary(Of String, String)

    Public Enum VideoCategories
        Musique
        Sports
        Gaming
        Education
        Films
        TVSeries
        Nouvelles
        Divertissement
        Maximum
    End Enum

    Public Const max_wait As Integer = 1200000 '20 minutes

    Public list_used_player() As String = {"no_integration", "legacy_wmp", "wmp", "embed", "video", "realplayer", "activex_realplayer", "embed_vlc", "vlc", "alt_vlc", "quicktime", "embed_quicktime", "flash", "embed_flash", "activex_flash", "object", "alt_video"}
    Public list_skin() As String = {"oldyt", "cosmic", "dark", "modern", "rose", "aqua", "monochrome", "mint", "sunshine"}
    Public list_playersize() As String = {"auto", "micro", "middle", "ultrasmall", "small", "large", "cinema", "bigcinema", "autoheight", "fullscreen", "fulljs", "gold1", "gold2", "cs", "vertical1", "vertical2", "eh", "ot", "otsh", "vertical3"}
    Public list_vsize() As String = {"vert1", "vert2", "vert3", "vert0"}
    Public list_usedcodec() As String = {"mpeg1", "avi_mpeg4", "avi_msvideo1", "avi_mjpeg", "mp4", "rm", "wmv2", "mov_cinepak", "mov_svq1", "3gp", "avi_yuv", "flv", "wmv1", "mov_mpeg4", "avi_cinepak", "mov_rpza", "mov_mjpeg", "xvid", "recent_mpeg1", "legacy_mp4"}
    Public list_framerate() As String = {"auto", "10", "12", "15", "20", "24", "25", "30", "60"}
    Public list_resolution() As String = {"auto", "96p", "120p", "144p", "240p", "360p", "480p", "720p", "1080p"}
    Public list_results() As String = {"1", "5", "10", "20"}
    Public list_coms() As String = {"0", "10", "20", "50", "100"}
    Public list_vids_channels() As String = {"9", "18", "27"}

    Public list_used_player_string() As String = {"Aucune intégration", "Windows Media Player 6.4 (ActiveX)", "Windows Media Player 7.0 ou plus (ActiveX)", "Intégration générique (Embarquée)", "Intégration vidéo HTML5", "Real Player (Embarqué)", "Real Player (ActiveX)", "Lecteur VLC (Embarqué)", "Lecteur VLC (ActiveX)", "Lecteur VLC (ActiveX avec un CLSID alternatif)", "Apple QuickTime (ActiveX)", "Apple QuickTime (Embarqué)", "Flash Player (Javascript)", "Flash Player (Embarqué)", "Flash Player (ActiveX)", "Intégration générique (Object)", "Vidéo HTML5 (Adaptée pour Android, Nintendo, PlayStation)"}
    Public list_skin_string() As String = {"Apparence classique", "Cosmic Tube", "Mode sombre", "Apparence moderne", "Thème rose", "Thème aquatique", "Apparence monochrome", "Thème menthe", "Thème doré"}
    Public list_playersize_string() As String = {"Automatique (Avec Javascript)", "Taille micro au format 4:3 (160x120)", "Taille standard VGA au format 4:3 (640x480)", "Taille ultra compacte en format 4:3 (256x144)", "Taille compacte QVGA au format 4:3 (320x240)", "Taille petit cinéma au format 16:9 (854x480)", "Taille cinéma standard au format 16:9 (1280x720)", "Taille grand cinéma au format 16:9 (2560x1440)", "Automatique (En fonction du ratio de la vidéo)", "Plein écran (Proportionnellement à la taille du rendu via HTML)", "Plein écran (Avec Javascript)", "Taille standard 16:10 (1280x800)", "Taille grand 16:10 (1440x900)", "Taille classique du lecteur YouTube au format 4:3 (480x360)", "Taille verticale classique en 9:16 (270x480)", "Taille verticale moyenne 9:16 (360x640)", "Taille moyenne en 4:3 (800x600)", "Grande taille en 4:3 (1024x768)", "Très grande taille en 4:3 (1600x1200)", "Taille verticale grande 9:16 (720x1280)"}
    Public list_vsize_string() As String = {"Petite taille verticale", "Moyenne taille verticale", "Grande taille verticale", "Micro taille verticale"}

    Public http_status_labels(1024) As String

    'Pied de page générique à certaines pages.
    Public footer As String = "<P ALIGN=CENTER STYLE=""display: block; background-color: black; color: white; border-radius: 4px; padding: 4px 4px 4px 4px; margin-left: auto; margin-right: auto; text-align: center; width: 580px;""><B>RetroYT Bêta 9.0</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.</P><P ALIGN=CENTER><A HREF=""/feed"" STYLE=""color: " & link_color & """>Index</A> - <A HREF=""/about.htm"" STYLE=""color: " & link_color & """>Informations</A> - <A HREF=""config.cgi"">Paramètres</A> - <A HREF=""debug.cgi"">Débogage</A> - <A HREF=""cache.cgi"">Cache des vidéos</A> - <A HREF=""/lucky"">Mode chanceux</A></P>" & vbCrLf &
    "<!-- Préchargement des images utilisées par les différents skins -->" & vbCrLf & "<IMG SRC=""btn_mint.png"" alt=""Button Mint Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_mint.png"" alt=""Button Mint Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_aqua.png"" alt=""Button Aqua Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_aqua.png"" alt=""Button Aqua Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_grad.png"" alt=""Button Red Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_grad.png"" alt=""Button Red Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_pink.png"" alt=""Button Pink Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_pink.png"" alt=""Button Pink Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_gold.png"" alt=""Button Gold Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_gold.png"" alt=""Button Gold Hot"" WIDTH=1 HEIGHT=1 />" & vbCrLf & "<BR><BR></BODY>" & vbCrLf & "</HTML>" & vbCrLf

    Public Const cookie_header As String = "retroyt="
    Public vt As RequestVideoType = RequestVideoType.WatchVideo
    Public shortdic As New Dictionary(Of String, ShortRelative)

    Public Enum RequestVideoType
        WatchVideo 'Regarder une vidéo directement, intégrée dans une page HTML.
        StreamVideo 'Regarder une vidéo directement, envoyée sous forme de flux.
        LuckyVideo 'Chercher un tag, et retourner la première vidéo trouvée.
        SearchVideo 'Chercher une vidéo et retourner une liste formatée en une page Web interprétable par un navigateur.
        ShortVideo 'Les vidéos courtes qui sont apparues sur YouTube vers la fin des années 2010.
    End Enum

    Public Class VideoProperties
        Public ID As String = String.Empty
        Public Title As String = "(Titre inconnu)"
        Public Dimensions As String = "640:480"
        Public Description As String = "Aucune description disponible."
        Public Creator As String = "(Créateur inconnu)"
        Public Channel_URL As String = "about:blank"
        Public DateOfRelease As String = "1 jan. 1970"
        Public Duration As Integer = -1
        Public Views As String = "0"
        Public DateAdded As Date = New Date(1970, 1, 1)
        Public Thumbnail As String = "about:blank"
        Public Like_Count As String = "na"
        Public Dislike_Count As String = "na"
    End Class

    Public Class ShortRelative
        Public UpShort As String = String.Empty
        Public DownShort As String = String.Empty
    End Class

    Public Class YoutubeComments
        Public text As String
        Public author As String
        Public like_count As Integer
        Public timestamp As Integer
        Public author_url As String
        Public author_thumbnail As String
    End Class

    Public Class OutputResponse
        Public OutputData As String = String.Empty
        Public ErrorData As String = String.Empty
        Public HasErrors As Boolean = False
        Public ExceptionMessage As String = "Aucun message spécifié."
    End Class

    Function IsNetworkAvailable() As Boolean
        Try
            Dim req = CType(WebRequest.Create("https://www.youtube.com/"), HttpWebRequest)
            req.Method = "HEAD"
            req.Timeout = 3000

            Using resp = req.GetResponse()
                Return True
            End Using
        Catch
            Return False
        End Try
    End Function

    Function GetHost() As String
        If String.IsNullOrEmpty(last_host) Then
            Return "/"
        End If

        If last_host.Length = 0 Then
            Return "/"
        End If

        Return "http://" & last_host & "/"
    End Function

    Function LaunchProcess(ByVal file_args As String, Optional ByVal file_name As String = "yt-dlp.exe", Optional lock_file As String = Nothing, Optional last_view As String = Nothing, Optional ByVal wait_delay As Integer = max_wait) As OutputResponse
        Dim op As New OutputResponse
        Dim psi As New ProcessStartInfo()
        Dim add_cookie As String = String.Empty
        psi.FileName = file_name
        psi.Arguments = file_args.Trim

        If psi.FileName = "yt-dlp.exe" Then
            If IO.File.Exists("cookies.txt") Then
                add_cookie &= " --cookies cookies.txt"
                WriteLog("Usage du fichier cookies.txt ajouté par l'administrateur du serveur pour récupérer les commentaires.", ConsoleColor.Magenta)
            End If
            psi.Arguments &= " --no-warnings --encoding utf-8 --user-agent ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 OPR/131.0.0.0""" & add_cookie
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.StandardOutputEncoding = Encoding.UTF8
            psi.StandardErrorEncoding = Encoding.UTF8
        End If

        psi.UseShellExecute = False
        psi.CreateNoWindow = True

        Try
            Dim p As Process = Process.Start(psi)

            If String.IsNullOrEmpty(lock_file) = False Then
                IO.File.WriteAllText(lock_file, p.Id.ToString & vbCrLf & GetMD5(last_view))
            End If

            If psi.FileName = "yt-dlp.exe" Then
                op.OutputData = p.StandardOutput.ReadToEnd()
                op.ErrorData = p.StandardError.ReadToEnd()
            End If

            p.WaitForExit(wait_delay)

            If Not String.IsNullOrEmpty(lock_file) Then
                IO.File.Delete(lock_file)
            End If
        Catch ex As Exception
            op.HasErrors = True
            op.ExceptionMessage = ex.Message
        End Try

        Return op
    End Function

    Function GetVideo(ByVal watcharg As String) As VideoProperties
        SyncLock video_props
            Dim tmp_prop As New VideoProperties

            If video_props.Count > 1000 Then
                Do Until video_props.Count = 1000
                    video_props.Remove(video_props.Keys(0))
                Loop
            End If

            If Not video_props.ContainsKey(watcharg) Then
                Dim add_cookie As String = String.Empty
                If IO.File.Exists("cookies.txt") Then
                    add_cookie &= " --cookies cookies.txt"
                    WriteLog("Usage du fichier cookies.txt ajouté par l'administrateur du serveur pour récupérer les commentaires.", ConsoleColor.Magenta)
                End If

                Dim get_video_info As OutputResponse = LaunchProcess("--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<|>%(channel_id)s<|>%(like_count)s<|>%(dislike_count)s"" ""https://www.youtube.com/watch?v=" & watcharg & """ --no-warnings --encoding utf-8 --user-agent ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 OPR/131.0.0.0""" & add_cookie)
                Dim output As String = get_video_info.OutputData
                output = output.Replace(vbLf, String.Empty)
                output = output.Replace(vbCr, String.Empty)
                If output.EndsWith("<|>") Then output = output.Remove(output.Length - 3, 3)

                Dim err As String = get_video_info.ErrorData
                Dim output_elements() As String = Nothing

                Try
                    output_elements = output.Split("<|>")

                    For i As Integer = 0 To output_elements.Count - 1
                        For j As Integer = 0 To &H1F
                            output_elements(i) = output_elements(i).Replace(Chr(j), String.Empty)
                        Next
                    Next

                    output_elements(9) = output_elements(9).Replace(vbCrLf, "<BR>")
                    output_elements(9) = output_elements(9).Replace(vbCr, "<BR>")
                    output_elements(9) = output_elements(9).Replace(vbLf, "<BR>")

                    tmp_prop.ID = output_elements(0)
                    tmp_prop.Title = CleanText(output_elements(1))
                    tmp_prop.Views = IIf(LCase(output_elements(2)) = "na", "0", GetThousands(output_elements(2)))
                    tmp_prop.DateOfRelease = GetDate(output_elements(3))
                    tmp_prop.Creator = CleanText(output_elements(4))
                    tmp_prop.Thumbnail = output_elements(5)
                    tmp_prop.Channel_URL = "/channel.cgi?id=" & CleanText(output_elements(10) & "&amp;section=videos")
                    tmp_prop.Like_Count = output_elements(11)
                    tmp_prop.Dislike_Count = output_elements(12)

                    If LCase(output_elements(6)) = "na" Then
                        tmp_prop.Duration = -1
                    Else
                        'tmp_prop.Duration = GetDuration(output_elements(6))
                        tmp_prop.Duration = CInt(output_elements(6))
                    End If

                    tmp_prop.Description = IIf(String.IsNullOrEmpty(output_elements(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(output_elements(9))))
                    If tmp_prop.Description.Length > 2048 Then tmp_prop.Description = tmp_prop.Description.Substring(0, 2048) & "..."
                    tmp_prop.Description = tmp_prop.Description.Replace(vbCrLf, "<BR>")
                    tmp_prop.Description = tmp_prop.Description.Replace(vbCr, "<BR>")
                    tmp_prop.Description = tmp_prop.Description.Replace(vbLf, "<BR>")

                    tmp_prop.Dimensions = IIf(IsNumeric(output_elements(7)), output_elements(7), 640) & ":" & IIf(IsNumeric(output_elements(8)), output_elements(8), 480)
                    tmp_prop.DateAdded = Now

                    video_props.Add(watcharg, tmp_prop)
                Catch ex As Exception

                End Try
            Else
                tmp_prop = video_props(watcharg)
            End If

            Return tmp_prop
        End SyncLock
    End Function

    Function UnicodeJson(ByVal t As String) As String
        'Faire l'échappement avant, histoire que le code HTML des émojis ne saute pas.
        t = EscapeHtml(t)

        If t.Contains("\u00") Then
            Dim first_u As Integer = 0
            Dim second_u As String = "00"
            Dim fc As Integer = 0

            Do
                'Conversion Unicode vers ISO
                first_u = t.IndexOf("\u00")
                If first_u = -1 Or first_u > t.Length - 6 Then Exit Do
                second_u = t.Substring(first_u + 4, 2)
                fc = CInt("&H" & second_u)

                If fc > &H1F And fc < &HFF Then
                    t = t.Substring(0, first_u) & ChrW(fc) & t.Substring(first_u + 6, t.Length - first_u - 6)
                Else
                    t = t.Substring(0, first_u) & t.Substring(first_u + 6, t.Length - first_u - 6)
                End If
            Loop
        End If

        'Certains caractères spéciaux
        t = t.Replace("\u0153", "oe")
        t = t.Replace("\u2019", "'")
        t = t.Replace("\u201c", """")
        t = t.Replace("\u201d", """")

        'Prise en charge des émojis -> smileys ASCII
        t = t.Replace("\ud83d\ude00", "<IMG SRC=""/e_smile1.gif"" ALT="":-)"" />")
        t = t.Replace("\ud83d\ude01", "<IMG SRC=""/e_smile2.gif"" ALT="":-D"" />")
        t = t.Replace("\ud83d\ude02", "<IMG SRC=""/e_laugh2.gif"" ALT="":')"" />")
        t = t.Replace("\ud83d\ude03", "<IMG SRC=""/e_smile2.gif"" ALT="":-D"" />")
        t = t.Replace("\ud83d\ude04", "<IMG SRC=""/e_blush1.gif"" ALT=""^_^"" />")
        t = t.Replace("\ud83d\ude05", "<IMG SRC=""/e_sweat1.gif"" ALT=""^_^'"" />")
        t = t.Replace("\ud83d\ude06", "<IMG SRC=""/e_laugh3.gif"" ALT=""[LAUGHING_EMOJI]"" />")
        t = t.Replace("\ud83d\ude07", "<IMG SRC=""/e_angel.gif"" ALT=""O:-)"" />")
        t = t.Replace("\ud83d\ude08", "<IMG SRC=""/e_robber.gif"" ALT="">:-)"" />")
        t = t.Replace("\ud83d\ude09", "<IMG SRC=""/e_wink1.gif"" ALT="";-)"" />")
        t = t.Replace("\ud83d\ude0a", "<IMG SRC=""/e_blush1.gif"" ALT=""^_^"" />")
        t = t.Replace("\ud83d\ude0b", "<IMG SRC=""/e_tongue.gif"" ALT=""[STICKING_OUT_TONGUE_EMOJI]"" />")
        t = t.Replace("\ud83d\ude0c", "<IMG SRC=""/e_relief.gif"" ALT=""|-)"" />")
        t = t.Replace("\ud83d\ude0d", "<IMG SRC=""/e_love1.gif"" ALT=""[LOVE_EYES_MOUTH_OPEN_EMOJI]"" />")
        t = t.Replace("\ud83d\ude0e", "<IMG SRC=""/e_sun1.gif"" ALT=""[SUNGLASSES_EMOJI]"" />")
        t = t.Replace("\ud83d\ude0f", "<IMG SRC=""/e_smirk.gif"" ALT=""[SMIRK_EMOJI]"" />")
        t = t.Replace("\ud83d\ude10", "<IMG SRC=""/e_neutr1.gif"" ALT="":-|"" />")
        t = t.Replace("\ud83d\ude11", "<IMG SRC=""/e_disapp.gif"" ALT=""-_-"" />")
        t = t.Replace("\ud83d\ude12", "<IMG SRC=""/e_frown2.gif"" ALT=""[UNAMUSED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude13", "<IMG SRC=""/e_sweat2.gif"" ALT=""-_-'"" />")
        t = t.Replace("\ud83d\ude14", "<IMG SRC=""/e_pensif.gif"" ALT=""[PENSIVE_EMOJI]"" />")
        t = t.Replace("\ud83d\ude15", "<IMG SRC=""/e_frown1.gif"" ALT="":-("" />")
        t = t.Replace("\ud83d\ude16", "<IMG SRC=""/e_conf2.gif"" ALT=""[CONFUSED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude17", "<IMG SRC=""/e_kiss1.gif"" ALT=""[KISS_EMOJI]"" />")
        t = t.Replace("\ud83d\ude18", "<IMG SRC=""/e_kiss2.gif"" ALT=""[KISS_EMOJI]"" />")
        t = t.Replace("\ud83d\ude19", "<IMG SRC=""/e_kiss2.gif"" ALT=""[KISS_EMOJI]"" />")
        t = t.Replace("\ud83d\ude1a", "<IMG SRC=""/e_kiss2.gif"" ALT=""[KISS_EMOJI]"" />")
        t = t.Replace("\ud83d\ude1b", "<IMG SRC=""/e_tongue.gif"" ALT=""[STICKING_OUT_TONGUE_EMOJI]"" />")
        t = t.Replace("\ud83d\ude1c", ";-P")
        t = t.Replace("\ud83d\ude1d", "x-P")
        t = t.Replace("\ud83d\ude1e", "<IMG SRC=""/e_pensif.gif"" ALT=""[PENSIVE_EMOJI]"" />")
        t = t.Replace("\ud83d\ude1f", "<:-(")
        t = t.Replace("\ud83d\ude20", "<IMG SRC=""/e_angry2.gif"" ALT=""[ANGRY_EMOJI]"" />")
        t = t.Replace("\ud83d\ude21", "<IMG SRC=""/e_angry2.gif"" ALT=""[VERY_ANGRY_EMOJI]"" />")
        t = t.Replace("\ud83d\ude22", ":'(")
        t = t.Replace("\ud83d\ude23", "<IMG SRC=""/e_frown4.gif"" ALT=""[FROWNING_EMOJI]"" />")
        t = t.Replace("\ud83d\ude24", "<IMG SRC=""/e_trmph1.gif"" ALT=""[TRIUMPH_EMOJI]"" />")
        t = t.Replace("\ud83d\ude25", "._.'")
        t = t.Replace("\ud83d\ude26", "D-:")
        t = t.Replace("\ud83d\ude27", "D-8")
        t = t.Replace("\ud83d\ude28", "[FEARFUL_EMOJI]")
        t = t.Replace("\ud83d\ude29", "x-O")
        t = t.Replace("\ud83d\ude2a", "<IMG SRC=""/e_sleep1.gif"" ALT=""[SLEEPY_EMOJI]"" />")
        t = t.Replace("\ud83d\ude2b", "[TIRED_EMOJI]")
        t = t.Replace("\ud83d\ude2c", "[GRIMACING_EMOJI]")
        t = t.Replace("\ud83d\ude2d", "<IMG SRC=""/e_cry1.gif"" ALT=""[HEAVILY_CRYING_EMOJI]"" />")
        t = t.Replace("\ud83d\ude2e", "<IMG SRC=""/e_shock1.gif"" ALT=""[SHOCKED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude2f", "<IMG SRC=""/e_shock1.gif"" ALT=""[SHOCKED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude30", "O_o'")
        t = t.Replace("\ud83d\ude31", "[SCREAMING_EMOJI]")
        t = t.Replace("\ud83d\ude32", "<IMG SRC=""/e_shock1.gif"" ALT=""[SHOCKED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude33", "<IMG SRC=""/e_flush1.gif"" ALT=""[FLUSHED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude34", "<IMG SRC=""/e_sleep1.gif"" ALT=""[SLEEPY_EMOJI]"" />")
        t = t.Replace("\ud83d\ude35", "<IMG SRC=""/e_confus.gif"" ALT=""[CONFUSED_EMOJI]"" />")
        t = t.Replace("\ud83d\ude36", "<IMG SRC=""/e_zipper.gif"" ALT=""[ZIPPER_MOUTH_EMOJI]"" />")
        t = t.Replace("\ud83d\ude37", "<IMG SRC=""/e_mask.gif"" ALT="":-[]"" />")
        t = t.Replace("\ud83d\ude41", "<IMG SRC=""/e_frown1.gif"" ALT="":-("" />")
        t = t.Replace("\ud83d\ude42", "<IMG SRC=""/e_smile1.gif"" ALT="":-)"" />")
        t = t.Replace("\ud83d\ude43", "<IMG SRC=""/e_upside.gif"" ALT=""[UPSIDE_DOWN_SMILE_EMOJI]"" />")
        t = t.Replace("\ud83d\udc7b", "<IMG SRC=""/e_ghost.gif"" ALT=""[GHOST_EMOJI]"" />")
        t = t.Replace("\ud83d\ude44", "<IMG SRC=""/e_roll1.gif"" ALT=""[ROLLING_EYES_EMOJI]"" />")
        t = t.Replace("\ud83e\udd21", "<IMG SRC=""/e_clown.gif"" ALT=""[CLOWN_EMOJI]"" />")
        t = t.Replace("\ud83d\udc8a", "<IMG SRC=""/e_pill.gif"" ALT=""[PILL_EMOJI]"" />")
        t = t.Replace("\ud83c\udfb5", "<IMG SRC=""/e_notes1.gif"" ALT=""[NOTES_EMOJI]"" />")
        t = t.Replace("\ud83c\udfb6", "<IMG SRC=""/e_notes2.gif"" ALT=""[NOTE_EMOJI]"" />")
        t = t.Replace("\ud83d\udc4f", "<IMG SRC=""/e_clap.gif"" ALT=""[CLAPPING_HANDS_EMOJI]"" />")
        t = t.Replace("\ud83d\ude4f", "<IMG SRC=""/e_pray.gif"" ALT=""[PRAY_EMOJI]"" />")
        t = t.Replace("\ud83d\udc4b", "<IMG SRC=""/e_wave.gif"" ALT=""[WAVING_HAND_EMOJI]"" />")

        t = t.Replace("\ud83e\udd10", "<IMG SRC=""/e_zipper.gif"" ALT=""[ZIPPER_MOUTH_EMOJI]"" />")
        t = t.Replace("\ud83e\udd11", "<IMG SRC=""/e_dollar.gif"" ALT=""[DOLLAR_FACE_EMOJI]"" />")
        t = t.Replace("\ud83e\udd12", "[ILL_EMOJI]")
        t = t.Replace("\ud83e\udd13", "<IMG SRC=""/e_nerd.gif"" ALT=""[NERD_EMOJI]"" />")
        t = t.Replace("\ud83e\udd14", "[THINKING_EMOJI]")
        t = t.Replace("\ud83e\udd15", "<IMG SRC=""/e_hurt1.gif"" ALT=""[HURT_EMOJI]"" />")
        t = t.Replace("\ud83e\udd17", "[HUGGING_EMOJI]")
        t = t.Replace("\ud83e\udd20", "]:-)")
        t = t.Replace("\ud83e\udd22", "[NAUSEATING_EMOJI]")
        t = t.Replace("\ud83e\udd23", "x')")
        t = t.Replace("\ud83e\udd24", "[DROOLING_EMOJI]")
        t = t.Replace("\ud83e\udd25", "<IMG SRC=""/e_lying.gif"" ALT=""[LYING_EMOJI]"" />")
        t = t.Replace("\ud83e\udd27", "[SNEEZING_EMOJI]")
        t = t.Replace("\ud83e\udd28", "<IMG SRC=""/e_frown3.gif"" ALT=""ò_Ô"" />")
        t = t.Replace("\ud83e\udd29", "*_*")
        t = t.Replace("\ud83e\udd2a", "[CRAZY_EMOJI]")
        t = t.Replace("\ud83e\udd2b", "[SHHH_EMOJI]")
        t = t.Replace("\ud83e\udd2d", "[CHUCKLING_EMOJI]")
        t = t.Replace("\ud83e\udd2e", "[VOMITING_EMOJI]")
        t = t.Replace("\ud83e\udd2f", "[EXPLODING_HEAD_EMOJI]")
        t = t.Replace("\ud83e\udd70", "<3")
        t = t.Replace("\ud83e\udd71", "[YAWNING_EMOJI]")
        t = t.Replace("\ud83e\udd73", "[PARTYING_EMOJI]")
        t = t.Replace("\ud83e\udd74", "<IMG SRC=""/e_drunk.gif"" ALT=""[DRUNK_EMOJI]"" />")
        t = t.Replace("\ud83e\udd75", "[HOT_EMOJI]")
        t = t.Replace("\ud83e\udd76", "<IMG SRC=""/e_frozen.gif"" ALT=""[FROZEN_EMOJI]"" />")
        t = t.Replace("\ud83e\udd7a", "<IMG SRC=""/e_tender.gif"" ALT=""[TENDER_EMOJI]"" />")
        t = t.Replace("\ud83e\uddd0", "<IMG SRC=""/e_monocl.gif"" ALT=""[MONOCLE_EMOJI]"" />")

        t = t.Replace("\ud83d\udc7f", "<IMG SRC=""/e_rob2.gif"" ALT="">:-("" />")
        t = t.Replace("\ud83d\udc80", "<IMG SRC=""/e_skull.gif"" ALT=""[SKULL_EMOJI]"" />")
        t = t.Replace("\ud83d\udd2c", "<IMG SRC=""/e_angry1.gif"" ALT=""[CENSORED_ANGRY_EMOJI]"" />")
        t = t.Replace("\ud83d\udd25", "<IMG SRC=""/e_fire.gif"" ALT=""[FIRE_EMOJI]"" />")
        t = t.Replace("\ud83d\udc4d", "<IMG SRC=""/e_thmbup.gif"" ALT=""[THUMBS_UP]"" />")
        t = t.Replace("\ud83d\udc4e", "<IMG SRC=""/e_thdown.gif"" ALT=""[THUMBS_DOWN]"" />")
        t = t.Replace("\ud83d\udd14", "<IMG SRC=""/e_bell.gif"" ALT=""[BELL_EMOJI]"" />")

        t = t.Replace("\u2764\ufe0f", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\ude77", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\udde1", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udc9b", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udc9a", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udc99", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\ude75", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udc9c", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\udd0e", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udda4", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\ude76", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83e\udd0d", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\u2665\ufe0f", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\ud83d\udc94", "<IMG SRC=""/e_broken.gif"" ALT=""[BROKEN_HEART_EMOJI]"" />")
        t = t.Replace("\u2764", "<IMG SRC=""/e_heart.gif"" ALT=""[HEART_EMOJI]"" />")
        t = t.Replace("\u26a0\ufe0f", "<IMG SRC=""/e_warn.gif"" ALT=""/!\"" />")

        'Emojis inconnus (c'est-à-dire plein d'entre eux)
        t = Regex.Replace(t, "\\ud83[d-e]\\[0-9a-f]{4}\\ud[c-f][0-9a-f]{3}", "[EMOJI]", RegexOptions.IgnoreCase)

        t = t.Trim
        't = EscapeHtml(t)
        t = t.Replace("\n", "<BR>")
        t = t.Replace("\r", String.Empty)

        'Virer les \u$$$$ restants, et les remplacer par un indicateur de caractère inconnu générique.
        If t.Contains("\u") Then
            Dim first_u As Integer = 0

            Do Until t.Contains("\u") = False
                first_u = t.IndexOf("\u")
                If first_u = -1 Then Exit Do
                t = t.Substring(0, first_u) & "&lt;?&gt;" & t.Substring(first_u + 6, t.Length - first_u - 6)
            Loop
        End If

        For i As Integer = 0 To &H1F
            t = t.Replace(Chr(i), String.Empty)
        Next

        Return t
    End Function

    Function InitValues(Optional ByVal t As String = Nothing, Optional ByVal k As String = Nothing, Optional ByVal wanted_skin As String = "cosmic", Optional ByVal lucky As Boolean = False, Optional ByVal uplayer As String = "na", Optional ByVal disp_search As Boolean = True, Optional ByVal large_footer As Boolean = False) As String
        System.Threading.Thread.Sleep(100)
        'Cette fonction génère une entête et un corps de page HTML de base à retourner au client.
        Dim total_page As String = String.Empty

        If uplayer = "video" Then
            total_page = "<!doctype html>" & vbCrLf
        Else
            total_page = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
        End If

        total_page &= "<HTML>" & vbCrLf
        total_page &= " <HEAD>" & vbCrLf

        If t = Nothing Then
            total_page &= "  <TITLE>RetroYT</TITLE>" & vbCrLf
        Else
            'Echappement des caractères pour éviter les bugs et les injections HTML.
            total_page &= "  <TITLE>RetroYT - " & EscapeHtml(t) & "</TITLE>" & vbCrLf
        End If

        total_page &= "  <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">" & vbCrLf
        total_page &= "  <META CHARSET=""iso-8859-1"" />" & vbCrLf
        total_page &= "  <META NAME=""viewport"" CONTENT=""width=device-width, initial-scale=1, minimum-scale=1"">" & vbCrLf
        total_page &= "  <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />" & vbCrLf
        total_page &= "  <LINK REL=""stylesheet"" TYPE=""text/css"" HREF=""style.css"" />" & vbCrLf
        total_page &= " </HEAD>" & vbCrLf & vbCrLf

        Select Case wanted_skin
            Case "dark"
                total_page &= "<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" LINK=""#E62727"" ALINK=""#E62727"" VLINK=""#E62727"">" & vbCrLf
                link_color = "#e62727"
            Case "cosmic"
                total_page &= "<BODY TEXT=""#000000"" BGCOLOR=""#EAEAEA"" LINK=""#B6262C"" ALINK=""#B6262C"" VLINK=""#B6262C"" BACKGROUND=""cosmic.gif"">" & vbCrLf
                link_color = "#1034be"
            Case "rose"
                total_page &= "<BODY TEXT=""#100010"" BGCOLOR=""#F2DEF2"" LINK=""#800080"" ALINK=""#800080"" VLINK=""#800080"">" & vbCrLf
                link_color = "#a0046b"
            Case "aqua"
                total_page &= "<BODY TEXT=""#0000F0"" BGCOLOR=""#ECFFFF"" LINK=""#2037A0"" ALINK=""#2037A0"" VLINK=""#2037A0"">" & vbCrLf
                link_color = "#1f38a0"
            Case "monochrome"
                total_page &= "<BODY TEXT=""#000000"" BGCOLOR=""#FFFFFF"" LINK=""#606060"" ALINK=""#606060"" VLINK=""#606060"">" & vbCrLf
                link_color = "#606060"
            Case "mint"
                total_page &= "<BODY TEXT=""#000000"" BGCOLOR=""#E8FFE8"" LINK=""#358832"" ALINK=""#358832"" VLINK=""#358832"">" & vbCrLf
                link_color = "#358832"
            Case "sunshine"
                total_page &= "<BODY TEXT=""#202000"" BGCOLOR=""#FFFDE7"" LINK=""#89800C"" ALINK=""#89800C"" VLINK=""#89800C"">" & vbCrLf
                link_color = "#89800c"
            Case Else
                total_page &= "<BODY TEXT=""#000000"" BGCOLOR=""#FFFFFF"" LINK=""#B6262C"" ALINK=""#B6262C"" VLINK=""#B6262C"">" & vbCrLf
                link_color = "#1034be"
        End Select

        If Not disp_search Then
            Return total_page
        End If

        Dim used_logo As String = "yt_logo2.gif"

        Select Case wanted_skin
            Case "oldyt" : used_logo = "yt_logo.gif"
            Case "cosmic" : used_logo = "yt_logo2.gif"
            Case "dark" : used_logo = "yt_dark.gif"
            Case "rose" : used_logo = "yt_rose.gif"
            Case "aqua" : used_logo = "yt_aqua.gif"
            Case "mint" : used_logo = "yt_mint.gif"
            Case "sunshine" : used_logo = "yt_gold.gif"
            Case "monochrome" : used_logo = "yt_mono.gif"
            Case Else : used_logo = "yt_modrn.gif"
        End Select

        'La tête de page pour rechercher des vidéos. Ce formulaire est présent sur chaque page naviguée.
        total_page &= vbCrLf

        If lucky Then
            total_page &= " <FORM METHOD=""GET"" ACTION=""/lucky"">" & vbCrLf
        Else
            total_page &= " <FORM METHOD=""GET"" ACTION=""/search"">" & vbCrLf
        End If

        total_page &= " <CENTER><BR><TABLE BORDER=0 ALIGN=CENTER CELLPADDING=4 CELLSPACING=0>" & vbCrLf
        total_page &= "  <TR>" & vbCrLf
        'patternpage &= "   <TD WIDTH=90>&nbsp;</TD>" & vbCrLf
        total_page &= "   <TD VALIGN=CENTER HEIGHT=80><A HREF=""/feed""><IMG SRC=""" & used_logo & """ BORDER=0 ALT=""Logo RetroYT"" HEIGHT=44 /></A></TD>" & vbCrLf

        If wanted_skin = "modern" Then
            total_page &= "   <TD VALIGN=CENTER HEIGHT=80><INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 300px;"" WIDTH=300 SIZE=54 MAXLENGTH=256 /></TD>" & vbCrLf
        Else
            total_page &= "   <TD VALIGN=CENTER HEIGHT=80><INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 310px;"" WIDTH=320 SIZE=56 MAXLENGTH=256 /></TD>" & vbCrLf
        End If

        If lucky Then
            total_page &= "   <TD VALIGN=CENTER HEIGHT=80><INPUT TYPE=""SUBMIT"" VALUE=""Mode chanceux"" CLASS=""red_button"" STYLE=""width: 120px;"" /></TD>" & vbCrLf
        Else
            total_page &= "   <TD VALIGN=CENTER HEIGHT=80><INPUT TYPE=""SUBMIT"" VALUE=""Rechercher"" CLASS=""red_button"" /></TD>" & vbCrLf
        End If

        total_page &= "  </TR>" & vbCrLf
        total_page &= " </TABLE><BR></CENTER>" & vbCrLf
        total_page &= " </FORM>" & vbCrLf

        If large_footer Then
            footer = "<P ALIGN=CENTER STYLE=""display: block; background-color: black; color: white; border-radius: 4px; padding: 8px 4px 8px 4px; margin-left: auto; margin-right: auto; text-align: center; width: 780px;""><B>RetroYT Bêta 9.0</B> Copyright &copy; 2026 Tous droits réservés.</P>"
        Else
            footer = "<P ALIGN=CENTER STYLE=""display: block; background-color: black; color: white; border-radius: 4px; padding: 8px 4px 8px 4px; margin-left: auto; margin-right: auto; text-align: center; width: 580px;""><B>RetroYT Bêta 9.0</B> Copyright &copy; 2026 Tous droits réservés.</P>"
        End If

        footer &= "<P ALIGN=CENTER><A HREF=""/feed"" STYLE=""color: " & link_color & """>Index</A> - <A HREF=""/about.htm"">Informations</A> - <A HREF=""config.cgi"">Paramètres</A> - <A HREF=""debug.cgi"">Débogage</A> - <A HREF=""cache.cgi"">Cache des vidéos</A> - <A HREF=""/lucky"">Mode chanceux</A></P>" & vbCrLf
        footer &= "<!-- Préchargement des images utilisées par les différents skins -->" & vbCrLf & "<IMG SRC=""btn_aqua.png"" alt=""Button Aqua Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_aqua.png"" alt=""Button Aqua Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_grad.png"" alt=""Button Red Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_grad.png"" alt=""Button Red Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_pink.png"" alt=""Button Pink Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_pink.png"" alt=""Button Pink Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_gold.png"" alt=""Button Gold Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_gold.png"" alt=""Button Gold Hot"" WIDTH=1 HEIGHT=1 />" & vbCrLf
        footer &= "<BR><BR></BODY>" & vbCrLf & "</HTML>" & vbCrLf
        Return total_page
    End Function

    Sub UpdateCache()
        Dim cache_dir As String = CurDir() & "\vidcache"
        Dim source_dir As String = CurDir() & "\srccache"

        If Not Directory.Exists(cache_dir) Then Exit Sub

        Dim files As List(Of FileInfo) = Directory.GetFiles(cache_dir).
        Select(Function(f) New FileInfo(f)).
        OrderBy(Function(fi) fi.LastWriteTime).ToList()

        Dim files_length As Long = files.Sum(Function(f) f.Length)

        Dim minFree As Long = 134217728 '132Mo
        Dim maxCache As Long = 17179869184 '16Go

        Dim freeSpace As Long = 0

        'Pour trouver le chemin racine où se situe l'application serveur
        For Each c As IO.DriveInfo In IO.DriveInfo.GetDrives()
            If LCase(CurDir()).StartsWith(LCase(c.RootDirectory.ToString)) Then
                freeSpace = c.AvailableFreeSpace
                Exit For
            End If
        Next

        'Suppression des fichiers vidéo anciens au-delà d'une certaine taille
        If files.Count > 0 AndAlso (freeSpace < minFree Or files_length > maxCache) Then
            For Each fi In files

                If freeSpace >= minFree And files_length <= maxCache Then Exit For

                Try
                    files_length -= fi.Length
                    freeSpace += fi.Length
                    fi.Delete()
                Catch

                End Try

            Next
        End If

        'Pareil pour le cache source (où les vidéos originales au format WebM/MP4 sont stockées)
        files = Directory.GetFiles(source_dir).
        Select(Function(f) New FileInfo(f)).
        OrderBy(Function(fi) fi.LastWriteTime).ToList()

        files_length = files.Sum(Function(f) f.Length)

        If files.Count > 0 AndAlso (freeSpace < minFree Or files_length > maxCache) Then
            For Each fi In files

                If freeSpace >= minFree And files_length <= maxCache Then Exit For

                Try
                    files_length -= fi.Length
                    freeSpace += fi.Length
                    fi.Delete()
                Catch

                End Try

            Next
        End If

        'Suppression des anciennes miniatures
        Dim thumbs As List(Of FileInfo) = Directory.GetFiles(CurDir() & "\thumbs").
        Select(Function(f) New FileInfo(f)).
        OrderBy(Function(fi) fi.LastWriteTime).ToList()

        If thumbs.Count > 1000 Then
            Do Until thumbs.Count = 1000
                Try
                    thumbs(0).Delete()
                    thumbs.RemoveAt(0)
                Catch ex As Exception
                    Exit Sub
                End Try
            Loop
        End If

        'Suppression des anciennes miniatures
        Dim pictmp As List(Of FileInfo) = Directory.GetFiles(CurDir() & "\tmp_pic").
        Select(Function(f) New FileInfo(f)).
        OrderBy(Function(fi) fi.LastWriteTime).ToList()

        If pictmp.Count > 1000 Then
            Do Until pictmp.Count = 1000
                Try
                    pictmp(0).Delete()
                    pictmp.RemoveAt(0)
                Catch ex As Exception
                    Exit Sub
                End Try
            Loop
        End If

        'On vire les fichiers vides
        Dim emfiles As String() = Directory.GetFiles(cache_dir)

        For Each p As String In emfiles
            If FileLen(p) = 0 Then
                Try
                    IO.File.Delete(p)
                Catch ex As Exception

                End Try
            End If
        Next

        'Pareil pour le cache source
        Dim esfiles As String() = Directory.GetFiles(source_dir)

        For Each p As String In esfiles
            If FileLen(p) = 0 Then
                Try
                    IO.File.Delete(p)
                Catch ex As Exception

                End Try
            End If
        Next
    End Sub

    Sub CleanupLock()
        'Suppression des fichiers de verrouillage, coupure du processus ffmpeg.exe associé, et suppression du fichier qui était en cours de création (pour éviter les conflits).
        Dim lock_dir As String = CurDir() & "\prclocks"

        For Each f As String In IO.Directory.GetFiles(lock_dir)
            f = f.Remove(0, lock_dir.Length + 1)
            Dim has_errors As Boolean = False

            If f.StartsWith("output_") And f.EndsWith(".lock") Then
                Dim proc_content As String = Nothing

                Try
                    proc_content = IO.File.ReadAllText(lock_dir & "\" & f)
                Catch ex As Exception
                    WriteLog("Impossible d'ouvrir le fichier " & f & ". Raisons: " & ex.Message, ConsoleColor.Red)
                    has_errors = True
                End Try

                If Not String.IsNullOrEmpty(proc_content) AndAlso proc_content.Contains(vbCrLf) Then
                    Dim proc_c() As String = proc_content.Split(vbCrLf)

                    If IsNumeric(proc_c(0)) And IO.File.Exists(CurDir() & "\vidcache\" & proc_c(1)) Then
                        Dim proc_id As Integer = CInt(proc_c(0))
                        Dim proc_delfile As String = CurDir() & "\vidcache\" & proc_c(1)
                        For Each pr1 As Process In System.Diagnostics.Process.GetProcesses
                            If pr1.Id = proc_id Then
                                Try
                                    pr1.Kill()
                                Catch ex As Exception
                                    WriteLog("Impossible de couper le processus 0x" & Hex(proc_id) & ". Raisons: " & ex.Message, ConsoleColor.Red)
                                    has_errors = True
                                End Try

                                Exit For
                            End If
                        Next

                        Try
                            If IO.File.Exists(proc_delfile) Then IO.File.Delete(proc_delfile)
                        Catch ex As Exception
                            WriteLog("Impossible de supprimer le fichier " & proc_delfile & ". Raisons: " & ex.Message, ConsoleColor.Red)
                            has_errors = True
                        End Try
                    End If
                End If

                Try
                    IO.File.Delete(lock_dir & "\" & f)
                Catch ex As Exception
                    WriteLog("Impossible de supprimer le fichier " & f & ". Raisons: " & ex.Message, ConsoleColor.Red)
                    has_errors = True
                End Try
            End If
        Next
    End Sub

    Sub CleanupDownload()
        'Supprimer tous les fichiers .lock correspondant aux téléchargements en cours d'exécution + le fichier qui était en cours de téléchargement + coupure du processus yt-dlp.
        Dim lock_dir As String = CurDir() & "\prclocks"

        For Each f As String In IO.Directory.GetFiles(lock_dir)
            f = f.Remove(0, lock_dir.Length + 1)

            If f.StartsWith("download_") And f.EndsWith(".lock") Then
                WriteLog("Un ancien processus de yt-dlp a été trouvé. Le proxy va tenter de le couper...")
                Dim d_l_lines() As String = IO.File.ReadAllLines(CurDir() & "\prclocks\" & f)
                Dim has_error As Boolean = False

                If d_l_lines.Count = 2 Then
                    If IsNumeric(d_l_lines(0)) Then
                        Dim found_prc As Boolean = False
                        For Each pk As Process In Diagnostics.Process.GetProcesses
                            If pk.Id.ToString = d_l_lines(0) Then
                                Try
                                    pk.Kill()
                                Catch ex As Exception
                                    WriteLog("Impossible de couper le processus de yt-dlp: " & ex.Message, ConsoleColor.Red)
                                    has_error = True
                                End Try

                                found_prc = True
                                Exit For
                            End If
                        Next

                        If Not found_prc And Not IsNumeric(d_l_lines(0)) Then WriteLog("Processus 0x" & Hex(d_l_lines(0)) & " introuvable.", ConsoleColor.Red)
                    End If

                    If IO.File.Exists(d_l_lines(1)) Then
                        Try
                            For Each ff As String In IO.Directory.GetFiles(CurDir() & "\srccache")
                                Dim orig_f As String = ff
                                ff = ff.Remove(0, Convert.ToString(CurDir() & "\srccache\").Length)
                                If LCase(ff).Contains(LCase(d_l_lines(1))) Then
                                    IO.File.Delete(orig_f)
                                End If
                            Next
                        Catch ex As Exception
                            WriteLog("Impossible d'effacer le fichier d'identifiant '" & d_l_lines(1) & "': " & ex.Message, ConsoleColor.Red)
                            has_error = True
                        End Try
                    End If
                End If

                Try
                    IO.File.Delete(CurDir() & "\prclocks\" & f)
                Catch ex As Exception
                    WriteLog("Impossible d'effacer le fichier " & f & ": " & ex.Message, ConsoleColor.Red)
                    has_error = True
                End Try

                If has_error = False And IsNumeric(d_l_lines(0)) Then WriteLog("Processus 0x" & Hex(d_l_lines(0)) & " coupé avec succès! Le fichier " & f & " a également été supprimé.", ConsoleColor.Green)
            End If
        Next
    End Sub

    Function GetClientIP(client As TcpClient) As String
        'Obtenir l'adresse IP du client
        Try
            If client.Client Is Nothing Then Return "0.0.0.0"
            Return CType(client.Client.RemoteEndPoint, IPEndPoint).Address.ToString()
        Catch ex As Exception
            Return "0.0.0.0"
        End Try
    End Function

    Function GetShortName(input As String) As String
        Dim crc As UInteger = 0

        For Each b As Byte In Encoding.ASCII.GetBytes(input)
            crc = crc Xor b
            For i As Integer = 0 To 7
                If (crc And 1) <> 0 Then
                    crc = &HEDB88320UI Xor (crc >> 1)
                Else
                    crc >>= 1
                End If
            Next
        Next

        Return crc.ToString("X8")
    End Function

    Function CleanText(input As String) As String
        Dim text As String = input

        text = text.Replace("’", "'")
        text = text.Replace("+", " ")
        text = text.Replace("|", "-")

        Try
            text = Uri.UnescapeDataString(text)
        Catch ex As Exception
            'Aucun décodage des échappements HTML
        End Try

        'Remove non Latin-1
        Dim sb As New Text.StringBuilder()
        For Each c As Char In text
            If AscW(c) >= 32 AndAlso AscW(c) <= 255 Then
                sb.Append(c)
            End If
        Next

        text = sb.ToString()

        'Normaliser les espaces
        Do While text.Contains("  ")
            text = text.Replace("  ", " ")
        Loop

        text = text.Trim()

        If String.IsNullOrEmpty(text) OrElse text.Length = 0 Then
            text = "(Sans titre)"
        End If

        Return text
    End Function

    Function GetDuration(ByVal i As Integer) As String
        'Fonction pour convertir les durées brutes en secondes, vers un format HH:mm:ss, ou mm:ss.
        Dim h As Integer = i \ 3600
        Dim m As Integer = (i Mod 3600) \ 60
        Dim s As Integer = i Mod 60

        If h > 0 Then
            Return h.ToString() & ":" & m.ToString("00") & ":" & s.ToString("00")
        Else
            Return m.ToString() & ":" & s.ToString("00")
        End If
    End Function

    Function GetDate(ByVal d As String) As String
        'Convertir une date au format yyyymmdd vers le format français (X mois Année)
        If d.Length <> 8 Then Return "1 jan. 1970"
        Dim y As String = d.Substring(0, 4)
        Dim months() As String = {"0", "jan.", "fév.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."}
        Dim m As Integer = CInt(d.Substring(4, 2))
        Dim j As String = d.Substring(6, 2)

        If j.StartsWith("0") Then j = j.Remove(0, 1)

        Return j & " " & months(m) & " " & y
    End Function

    Function GetThousands(ByVal v As String) As String
        'Séparation des milliers, encore selon le format français.
        If String.IsNullOrEmpty(v) Then Return "0"
        If Not IsNumeric(v) Then Return v.Trim

        Dim result As String = String.Empty

        While v.Length > 3
            result = " " & v.Substring(v.Length - 3) & result
            v = v.Substring(0, v.Length - 3)
        End While

        result = v & result

        Return result.Trim()
    End Function

    Function EscapeHtml(ByVal h As String) As String
        'Conversion des caractères cyrilliques
        h = h.Replace("а", "a")
        h = h.Replace("б", "b")
        h = h.Replace("в", "v")
        h = h.Replace("г", "g")
        h = h.Replace("д", "d")
        h = h.Replace("е", "e")
        h = h.Replace("ё", "ë")
        h = h.Replace("ж", "zh")
        h = h.Replace("з", "z")
        h = h.Replace("и", "i")
        h = h.Replace("й", "j")
        h = h.Replace("к", "k")
        h = h.Replace("л", "l")
        h = h.Replace("м", "m")
        h = h.Replace("н", "n")
        h = h.Replace("о", "o")
        h = h.Replace("п", "p")
        h = h.Replace("р", "r")
        h = h.Replace("с", "s")
        h = h.Replace("т", "t")
        h = h.Replace("у", "u")
        h = h.Replace("ф", "f")
        h = h.Replace("х", "kh")
        h = h.Replace("ц", "ts")
        h = h.Replace("ч", "ch")
        h = h.Replace("ш", "sh")
        h = h.Replace("щ", "shch")
        h = h.Replace("ъ", """")
        h = h.Replace("ы", "y")
        h = h.Replace("ь", "'")
        h = h.Replace("э", "ye")
        h = h.Replace("ю", "yu")
        h = h.Replace("я", "ya")

        h = h.Replace("А", "A")
        h = h.Replace("Б", "B")
        h = h.Replace("В", "V")
        h = h.Replace("Г", "G")
        h = h.Replace("Д", "D")
        h = h.Replace("Е", "E")
        h = h.Replace("Ё", "Ë")
        h = h.Replace("Ж", "ZH")
        h = h.Replace("З", "Z")
        h = h.Replace("И", "I")
        h = h.Replace("Й", "J")
        h = h.Replace("К", "K")
        h = h.Replace("Л", "L")
        h = h.Replace("М", "M")
        h = h.Replace("Н", "N")
        h = h.Replace("О", "O")
        h = h.Replace("П", "P")
        h = h.Replace("Р", "R")
        h = h.Replace("С", "S")
        h = h.Replace("Т", "T")
        h = h.Replace("У", "U")
        h = h.Replace("Ф", "F")
        h = h.Replace("Х", "KH")
        h = h.Replace("Ц", "TS")
        h = h.Replace("Ч", "CH")
        h = h.Replace("Ш", "SH")
        h = h.Replace("Щ", "SHCH")
        h = h.Replace("Ъ", """")
        h = h.Replace("Ы", "Y")
        h = h.Replace("Ь", "'")
        h = h.Replace("Э", "E")
        h = h.Replace("Ю", "YU")
        h = h.Replace("Я", "YA")

        'Les caractères inutiles ou qui peuvent menacer la sécurité du visualisateur.
        h = h.Replace("<", "&lt;")
        h = h.Replace(">", "&gt;")
        h = h.Replace("""", "&quot;")

        Return h
    End Function

    Function LooksLikeYoutubeID(id As String) As Boolean
        'Si l'ID communiqué est digne de YouTube, ou un truc pipé.
        If id.Length <> 11 Then Return False
        Return Regex.IsMatch(id, "^[a-zA-Z0-9_-]+$")
    End Function

    Sub WriteLog(ByVal line As String, Optional ByVal clr As ConsoleColor = ConsoleColor.Gray, Optional ByVal c As TcpClient = Nothing)
        SyncLock log_lock
            Dim f As String = Nothing

            'Console.BackgroundColor = ConsoleColor.Gray
            Console.ForegroundColor = ConsoleColor.White

            If (c Is Nothing) Then
                f = "Le " & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & " :"
            Else
                f = "Requête envoyée le " & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & " par " & GetClientIP(c) & " :"
            End If

            Console.WriteLine(f & Space(Console.BufferWidth - f.Length))
            Console.ForegroundColor = clr
            Console.WriteLine(line)
            Console.ForegroundColor = ConsoleColor.Gray

            If line.Length < Console.BufferWidth Then
                Console.WriteLine(Space(Console.BufferWidth - line.Length))
            Else
                Dim maxl As Integer = line.Length Mod Console.BufferWidth
                Console.WriteLine(Space(Console.BufferWidth - maxl))
            End If

            If number_of_dls + number_of_vids > 0 Then
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("Une tâche est en cours d'exécution. Veuillez ne pas fermer la fenêtre.")
            Else
                Console.ForegroundColor = ConsoleColor.White
                Console.WriteLine("Serveur en cours d'exécution. Appuyez sur CTRL+C pour quitter.")
            End If

            Console.CursorTop -= 1
            Console.ForegroundColor = ConsoleColor.Gray

            Try
                IO.File.AppendAllText("srvlogs\retroyt_server_" & DateTime.Now.ToString("dd-MM-yyyy") & ".log", f.Trim & " " & line.Trim & vbCrLf)
            Catch ex As Exception
                'Console.WriteLine("ERREUR INTERNE: Impossible d'écrire dans le fichier LOG.")
            End Try
        End SyncLock
    End Sub

    Function GetMD5(text As String) As String
        Dim md5 As MD5 = MD5.Create()
        Dim bytes() As Byte = Encoding.UTF8.GetBytes(text)
        Dim hash() As Byte = md5.ComputeHash(bytes)

        Dim sb As New StringBuilder()

        For Each b As Byte In hash
            sb.Append(b.ToString("X2"))
        Next

        Return sb.ToString()
    End Function

    Function GetHTTPBytes(ByVal status As Integer, ByVal message As String, Optional ByVal http_version As String = "1.0")
        Dim http_response As String =
        "HTTP/" & http_version & " " & status.ToString & " " & http_status_labels(status) & vbCrLf &
        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
        "Content-Length: " & iso.GetBytes(message).Length.ToString & vbCrLf &
        "Connection: close" & vbCrLf &
        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & message

        WriteLog("Erreur HTTP #" & status.ToString & " (" & http_status_labels(status) & ") renvoyée au client.", ConsoleColor.Red)

        Return iso.GetBytes(http_response)
    End Function

    Sub Main(args As String())

        For i As Integer = 0 To 1024
            http_status_labels(i) = "No Status Message Provided"
        Next

        http_status_labels(200) = "OK"
        http_status_labels(301) = "Moved Permanently"
        http_status_labels(302) = "Found"
        http_status_labels(400) = "Bad Request"
        http_status_labels(401) = "Unauthorized"
        http_status_labels(403) = "Forbidden"
        http_status_labels(404) = "Not Found"
        http_status_labels(409) = "Conflict"
        http_status_labels(410) = "Gone"
        http_status_labels(413) = "Content Too Large"
        http_status_labels(414) = "URI Too Long"
        http_status_labels(415) = "Unsupported Media Type"
        http_status_labels(416) = "Range Not Satisfiable"
        http_status_labels(418) = "I'm a teapot"
        http_status_labels(429) = "Too Many Requests"
        http_status_labels(500) = "Internal Server Error"
        http_status_labels(501) = "Not Implemented"
        http_status_labels(502) = "Bad Gateway"
        http_status_labels(503) = "Service Unavailable"
        http_status_labels(507) = "Insufficient Storage"

        'L'application démarre ici!
        Console.Title = "RetroYT"

        Console.Clear()
        Console.WriteLine()
        Console.WriteLine()
        Console.BackgroundColor = ConsoleColor.Gray
        Console.ForegroundColor = ConsoleColor.Black
        Console.WriteLine(Space(Console.BufferWidth))
        Console.Write("  RetroYT Bêta 9.0 - Copyright (c) 2026 Monokeros - Tous droits réservés.")
        Console.WriteLine(Space(Console.BufferWidth - 73))
        Console.WriteLine(Space(Console.BufferWidth))
        Console.WriteLine()
        Console.WriteLine()

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.Gray

        WriteLog("Initialisation du serveur mandataire en cours...")

        If Not IO.File.Exists("yt-dlp.exe") Then
            WriteLog("yt-dlp.exe est absent dans le dossier courant. Exécution impossible.")
            WriteLog("Veuillez placer yt-dlp.exe dans sa dernière version dans le dossier " & CurDir())
            Console.ReadKey()
            End
        End If

        If Not IO.File.Exists("ffmpeg.exe") Then
            WriteLog("ffmpeg.exe est absent dans le dossier courant. Exécution impossible.")
            WriteLog("Veuillez installer la dernière version de FFMPEG dans le dossier " & CurDir())
            Console.ReadKey()
            End
        End If

        sw = New Stopwatch
        sw.Reset()

        'Vérification du paramètre de la ligne de commande
        If Not String.IsNullOrEmpty(Environment.CommandLine) And Environment.GetCommandLineArgs.Count > 1 Then
            WriteLog("Application démarrée avec pour argument: " & Environment.GetCommandLineArgs(1))
            Dim portstring As String = Environment.GetCommandLineArgs(1)

            If IsNumeric(portstring) Then
                Try
                    Dim tmp_port As Integer = CInt(portstring)

                    If tmp_port < 1 Or tmp_port > 65535 Then
                        WriteLog("Le numéro de port spécifié en paramètre est invalide. Il doit être compris entre 1 et 65535.")
                    Else
                        port = tmp_port
                    End If
                Catch ex As Exception
                    WriteLog("Numéro de port illégal. Il doit être compris entre 1 et 65535.")
                    Console.ReadKey()
                    End
                End Try
            Else
                WriteLog("Impossible de changer le numéro de port, car le paramètre '" & portstring & "' n'est pas un entier valide.")
            End If
        Else
            WriteLog("Aucun numéro de port spécifié en ligne de commande. Démarrage sur le port par défaut (80).")
            WriteLog("Pour changer le numéro, lancez RetroYT avec le numéro de port en paramètre immédiat.")
        End If

        'Création des dossiers nécessaires à l'exécution du programme
        If Not IO.Directory.Exists(CurDir() & "\data") Then IO.Directory.CreateDirectory(CurDir() & "\data")
        If Not IO.Directory.Exists(CurDir() & "\thumbs") Then IO.Directory.CreateDirectory(CurDir() & "\thumbs")
        If Not IO.Directory.Exists(CurDir() & "\vidcache") Then IO.Directory.CreateDirectory(CurDir() & "\vidcache")
        If Not IO.Directory.Exists(CurDir() & "\srccache") Then IO.Directory.CreateDirectory(CurDir() & "\srccache")
        If Not IO.Directory.Exists(CurDir() & "\srvlogs") Then IO.Directory.CreateDirectory(CurDir() & "\srvlogs")
        If Not IO.Directory.Exists(CurDir() & "\prclocks") Then IO.Directory.CreateDirectory(CurDir() & "\prclocks")
        If Not IO.Directory.Exists(CurDir() & "\comments") Then IO.Directory.CreateDirectory(CurDir() & "\comments")
        If Not IO.Directory.Exists(CurDir() & "\tmp_pic") Then IO.Directory.CreateDirectory(CurDir() & "\tmp_pic")

        'Nettoyer les fichiers en cours de décodage
        CleanupLock()
        CleanupDownload()
        UpdateCache()

        WriteLog("Base de données de vidéos en cours de construction, veuillez patienter... Cela peut prendre quelques minutes.")

        Dim fm As Integer = 0

        'Formation d'une base de données de vidéos pour l'index principal
        While Not IsNetworkAvailable()
            fm += 1
            If fm >= 2 Then Console.CursorTop -= 3
            WriteLog("Réseau Internet indisponible. Veuillez le rétablir afin de pouvoir continuer à utiliser RetroYT.          ")
            System.Threading.Thread.Sleep(10000)
        End While

        For i As Integer = 0 To VideoCategories.Maximum - 1
            Dim asked_tag As String = "actualités"
            Select Case CType(i, VideoCategories)
                Case VideoCategories.Divertissement
                    asked_tag = "divertissement"
                Case VideoCategories.Education
                    asked_tag = "éducation"
                Case VideoCategories.Films
                    asked_tag = "films"
                Case VideoCategories.Gaming
                    asked_tag = "gaming"
                Case VideoCategories.Musique
                    asked_tag = "musique"
                Case VideoCategories.Nouvelles
                    asked_tag = "nouvelles"
                Case VideoCategories.Sports
                    asked_tag = "sports"
                Case VideoCategories.TVSeries
                    asked_tag = "podcasts"
            End Select

            Dim op_main_stream As OutputResponse = LaunchProcess("--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<|>%(channel_id)s<|>%(like_count)s<|>%(dislike_count)s<||>"" ""ytsearch9:" & asked_tag & """")
            Dim output As String = op_main_stream.OutputData 'Récupération des résultats
            output = output.Replace(vbLf, String.Empty)
            output = output.Replace(vbCr, String.Empty)
            If output.EndsWith("<||>") Then output = output.Remove(output.Length - 4, 4)
            index_streams.Add(CType(i, VideoCategories), output) 'Ajout de vidéos en cache
        Next

        'Démarrage du serveur
        Dim listener As New TcpListener(IPAddress.Any, port)

        Try
            listener.Start()
        Catch ex As Exception
            WriteLog("Impossible de démarrer le serveur sur le port spécifié. Raison: " & ex.Message, ConsoleColor.Red)
            Console.ReadKey()
            End
        End Try

        WriteLog("Serveur lancé sur le port " & port.ToString & " avec succès! En attente de connexions entrantes...")
        up_since = Now

        If port = 80 Then
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1/")
        Else
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1:" & port.ToString & "/")
        End If

        sw.Start()

        While True
            Dim client As TcpClient = listener.AcceptTcpClient()
            Dim t As New Threading.Thread(Sub() HandleClient(client))
            t.Start()

            If sw.ElapsedMilliseconds > 60000 Then
                Try
                    If ip_list.Count > 0 Then
                        For Each p As String In ip_list.Keys.ToList()
                            If ip_list(p) > 300 Then
                                ip_list(p) = -1 'L'IP est bannie, si en une minute, le client dépasse 300 requêtes HTTP.
                            Else
                                ip_list(p) = 0 'Réinitialisation du compteur
                            End If
                        Next
                    End If
                Catch ex As Exception

                End Try

                sw.Restart()
            End If
        End While
    End Sub

    Function UrlDecodeLatin1(input As String) As String
        Dim bytes As New List(Of Byte)

        Dim i As Integer = 0
        While i < input.Length
            If input(i) = "%"c AndAlso i + 2 < input.Length Then
                Dim hex = input.Substring(i + 1, 2)
                bytes.Add(Convert.ToByte(hex, 16))
                i += 3
            ElseIf input(i) = "+"c Then
                bytes.Add(32) 'Ajout d'un espace
                i += 1
            Else
                bytes.Add(CByte(AscW(input(i))))
                i += 1
            End If
        End While

        Return iso.GetString(bytes.ToArray())
    End Function

    Function ConvertTo43(ByVal w As Integer, ByVal h As Integer) As Size
        If w <= 0 Then Return New Size(0, 0)
        If h <= 0 Then Return New Size(0, 0)

        Do Until w / h <= 4 / 3
            w -= 1
        Loop
        'w = (h * 4) / 3
        If w Mod 2 = 1 Then w += 1

        Return New Size(w, h)
    End Function

    Function ConvertTo169(ByVal w As Integer, ByVal h As Integer) As Size
        If w <= 0 Then Return New Size(0, 0)
        If h <= 0 Then Return New Size(0, 0)

        Do Until w / h >= 16 / 9
            w += 1
        Loop
        'w = (h * 16) / 9
        If w Mod 2 = 1 Then w += 1

        Return New Size(w, h)
    End Function

    Sub HandleClient(client As TcpClient)
        'Variables
        Dim player_size As String = "cs" 'Paramètres par défaut
        Dim player_vsize As String = "vert1"
        Dim used_codec As String = "avi_mpeg4"
        Dim used_player As String = "embed"
        Dim used_resolution As String = "auto"
        Dim frame_rate As String = "auto"
        Dim wanted_skin As String = "cosmic"
        Dim number_of_results As Integer = 10
        Dim http_ver As String = "1.0"
        Dim using_vlc As Boolean = False
        Dim old_ie As Boolean = False
        Dim current_cookie As String = String.Empty
        Dim ua_string As String = String.Empty
        Dim right_panel As Boolean = True
        Dim disp_comments_per_video As Integer = 20
        Dim disp_vids_per_channel As Integer = 18
        Dim display_trends As Boolean = False
        Dim display_stream_button As Boolean = True
        Dim forbid_long_vids As Boolean = True
        Dim is_mdx As Boolean = False
        Dim patternpage As New StringBuilder

        'Prise en charge des requêtes par le client
        System.Threading.Thread.Sleep(50)
        client.ReceiveTimeout = 5000
        Dim stream = client.GetStream()

        'Lire la requête HTTP
        Dim buffer(8192) As Byte
        Dim bytesRead As Object = Nothing

        Try
            bytesRead = stream.Read(buffer, 0, buffer.Length)
        Catch ex As Exception
            client.Close()
            Exit Sub
        End Try

        Dim request As String = iso.GetString(buffer, 0, bytesRead)
        range_begin = -1
        range_end = -1
        number_of_results = 10

        SyncLock ip_list
            If Not ip_list.ContainsKey(GetClientIP(client)) Then
                ip_list.Add(GetClientIP(client), 1) 'Accueillir un nouveau client
            Else
                If ip_list(GetClientIP(client)) = -1 Then
                    'Ce flag indique que l'IP a été bannie.
                    Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes simultanées</H1>" & vbCrLf & "<P>Le serveur a détecté que vous avez envoyé trop de requêtes en une minute.<BR><BR>Votre adresse IP sera donc bannie pour toute cette session.</P>" & vbCrLf)

                    Try
                        stream.Write(ise_data, 0, ise_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try

                    client.Close()
                    Exit Sub
                Else
                    ip_list(GetClientIP(client)) += 1 'Une requête de plus comptabilisée.
                End If
            End If
        End SyncLock

        If String.IsNullOrEmpty(request) Then
            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête vide</H1>" & vbCrLf & "<P>La requête HTTP est vide, et ne peut donc être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

            Try
                If Not using_vlc Then stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
            Exit Sub
        End If

        'Traitement ligne par ligne des requêtes HTTP

        Dim rq As String = request
        If rq.Contains(vbCrLf & vbCrLf) Then
            rq = request.Substring(0, request.IndexOf(vbCrLf & vbCrLf))
        End If

        Dim header_list As New List(Of String)

        rq = rq.Replace(vbLf, vbCrLf) 'Compatibilité avec anciens navigateurs qui n'envoient que \n et pas \r\n.

        If Not rq.Contains(vbCrLf) Then
            header_list.Add(rq)
        Else
            header_list.AddRange(rq.Split(vbCrLf))
        End If

        Dim bad_cookie As Boolean = False

        If header_list(0).Contains("HTTP/") Then
            If header_list(0).EndsWith("HTTP/1.0") Then
                http_ver = "1.0"
            Else
                http_ver = "1.1"
            End If
        Else
            '0.9
            http_ver = "1.0"
        End If

        For Each l As String In header_list
            Dim tete, cntnu As String
            If l.Contains(":") Then
                tete = l.Substring(0, l.IndexOf(":")).Trim 'l.Split(":")(0).Trim
                cntnu = l.Substring(l.IndexOf(":") + 1, l.Length - 1 - l.IndexOf(":")).Trim 'l.Split(":")(1).Trim

                For x As Integer = 0 To &H1F 'Retirer les caractères systèmes, souvent synonymes de tentatives de hack mémoire
                    cntnu = cntnu.Replace(Chr(x), String.Empty)
                Next

                Select Case LCase(tete)
                    Case "cookie"
                        current_cookie = cntnu
                        If cntnu.Contains("=") And cntnu.Contains("&") And cntnu.StartsWith(cookie_header) Then
                            'WriteLog("Cookie envoyé par le client: " & cntnu, ConsoleColor.Yellow, client)

                            cntnu = cntnu.Remove(0, cookie_header.Length) 'Retirer retroyt=
                            If cntnu.Contains(";") Then cntnu = cntnu.Substring(0, cntnu.IndexOf(";"))
                            Dim c_params() As String = Split(cntnu, "&")

                            For Each c_param As String In c_params
                                Dim p1, p2 As String
                                p1 = LCase(c_param.Split("=")(0))
                                p2 = LCase(c_param.Split("=")(1))

                                Select Case p1
                                    Case "player"
                                        If list_used_player.Contains(p2) Then
                                            used_player = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "size"
                                        If list_playersize.Contains(p2) Then
                                            player_size = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "vsize"
                                        If list_vsize.Contains(p2) Then
                                            player_vsize = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "codec"
                                        If list_usedcodec.Contains(p2) Then
                                            used_codec = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "framerate"
                                        If list_framerate.Contains(p2) Then
                                            If p2 = "auto" Then
                                                frame_rate = "auto"
                                            Else
                                                frame_rate = CInt(p2)
                                            End If
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "resolution"
                                        If list_resolution.Contains(p2) Then
                                            used_resolution = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "skin"
                                        If list_skin.Contains(p2) Then
                                            wanted_skin = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "results"
                                        If list_results.Contains(p2) AndAlso IsNumeric(p2) Then
                                            Try
                                                number_of_results = CInt(p2)
                                                If number_of_results > 20 Then number_of_results = 20 : bad_cookie = True
                                                If number_of_results < 1 Then number_of_results = 1 : bad_cookie = True
                                            Catch ex As Exception
                                                number_of_results = 10 'Au cas où une information erronée aurait été renseignée
                                                bad_cookie = True
                                            End Try
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "panel"
                                        If p2 = "false" Then
                                            right_panel = False
                                        ElseIf p2 = "true" Then
                                            right_panel = True
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "displaycomments"
                                        If IsNumeric(p2) Then
                                            Try
                                                Dim disp_comments As Integer = CInt(p2)
                                                If Not list_coms.Contains(p2) Then
                                                    bad_cookie = True
                                                Else
                                                    disp_comments_per_video = disp_comments
                                                End If
                                            Catch ex As Exception
                                                bad_cookie = True
                                            End Try
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "vcn"
                                        If IsNumeric(p2) Then
                                            Try
                                                Dim disp_vid_channels As Integer = CInt(p2)
                                                If Not list_vids_channels.Contains(p2) Then
                                                    bad_cookie = True
                                                Else
                                                    disp_vids_per_channel = disp_vid_channels
                                                End If
                                            Catch ex As Exception
                                                bad_cookie = True
                                            End Try
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "trends"
                                        If p2 = "enable" Then
                                            display_trends = True
                                        ElseIf p2 = "disable" Then
                                            display_trends = False
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "displaystream"
                                        If p2 = "enable" Then
                                            display_stream_button = True
                                        ElseIf p2 = "disable" Then
                                            display_stream_button = False
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "hidelongvids"
                                        If p2 = "yes" Then
                                            forbid_long_vids = True
                                        ElseIf p2 = "no" Then
                                            forbid_long_vids = False
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case Else
                                        'Les autres paramètres sont considérés comme faux.
                                        bad_cookie = True
                                End Select
                            Next
                        End If
                    Case "user-agent"
                        'L'agent utilisateur du navigateur client
                        'WriteLog("Agent utilisateur: " & cntnu, ConsoleColor.Blue, client)
                        ua_string = cntnu

                        If LCase(cntnu).StartsWith("vlc/3.0") Or LCase(cntnu).StartsWith("vlc/2.0") Then
                            using_vlc = True
                        Else
                            using_vlc = False
                        End If

                        If LCase(cntnu).Contains("msie 2.") Or LCase(cntnu).Contains("msie 3.") Or LCase(cntnu).Contains("msie 4.") Or LCase(cntnu).Contains("msie 5.") Or LCase(cntnu).Contains("msie 6.") Or LCase(cntnu).Contains("msie 7.") Then
                            old_ie = True
                        Else
                            old_ie = False
                        End If

                        If LCase(cntnu).Contains("mdxengine") Then
                            is_mdx = True
                        End If
                    Case "host"
                        'Dernier nom de domaine exploré par le client
                        last_host = cntnu
                    Case "range"
                        'Reprise d'une lecture en plein milieu en précisant à partir de quel octet reprendre (n'est curieusement jamais appelé par les navigateurs)
                        If LCase(cntnu.StartsWith("bytes=")) Then
                            cntnu = cntnu.Remove(0, 5)
                            If cntnu.Contains(",") Then cntnu = cntnu.Substring(0, cntnu.IndexOf(","))

                            If cntnu = "-" Then
                                Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                Try
                                    stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                Catch ey As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                End Try

                                client.Close()
                                Exit Sub
                            ElseIf cntnu.EndsWith("-") Then
                                'D'un offset indiqué, jusqu'à la fin du fichier
                                cntnu = cntnu.Replace("-", String.Empty)
                                Dim echec As Boolean = False

                                If String.IsNullOrEmpty(cntnu) Then
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                    Try
                                        stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                    Catch ey As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                Else
                                    If IsNumeric(cntnu) Then
                                        Try
                                            Dim tmp_offset As Long = CLng(cntnu.Trim)

                                            If tmp_offset < 0 Or tmp_offset > 34359738368 Then 'Limiter à 32Go
                                                echec = True
                                            Else
                                                range_begin = tmp_offset
                                                range_end = -2 'Fin encore inconnue
                                            End If
                                        Catch ex As Exception
                                            echec = True
                                        End Try

                                        If echec Then
                                            'Lever une erreur
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                            Try
                                                stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                            Catch ey As Exception
                                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                            End Try

                                            client.Close()
                                            Exit Sub
                                        End If
                                    End If
                                End If
                            ElseIf cntnu.StartsWith("-") Then
                                'Les X derniers octets

                                cntnu = cntnu.Remove(0, 1)
                                Dim echec As Boolean = False

                                If IsNumeric(cntnu) Then
                                    Try
                                        Dim end_offset As Long = CLng(cntnu)

                                        If end_offset < 0 Or end_offset > 34359738368 Then
                                            echec = True
                                        Else
                                            range_begin = -2
                                            range_end = end_offset
                                        End If
                                    Catch ex As Exception
                                        echec = True
                                    End Try
                                Else
                                    echec = True
                                End If

                                If echec Then
                                    'Lever une erreur
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                    Try
                                        stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                    Catch ey As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                End If
                            Else
                                'D'un offset A à un offset B dans le fichier
                                If cntnu.Contains("-") Then
                                    Dim param1, param2 As String
                                    param1 = cntnu.Split("-")(0)
                                    param2 = cntnu.Split("-")(1)

                                    If Not IsNumeric(param1) Or Not IsNumeric(param2) Then
                                        'Lever une erreur
                                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                        Try
                                            stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                        Catch ey As Exception
                                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                        End Try

                                        client.Close()
                                        Exit Sub
                                    Else

                                        Dim echec As Boolean = False

                                        Try
                                            Dim lng_param1, lng_param2 As Long
                                            lng_param1 = CLng(param1)
                                            lng_param2 = CLng(param2)

                                            If lng_param1 >= lng_param2 Or lng_param1 < 0 Or lng_param2 > 34359738368 Then
                                                echec = True
                                            Else
                                                range_begin = lng_param1
                                                range_end = lng_param2
                                            End If

                                        Catch ex As Exception
                                            'Lever une erreur
                                            echec = True
                                        End Try

                                        If echec Then
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                            Try
                                                stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                            Catch ey As Exception
                                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                            End Try

                                            client.Close()
                                            Exit Sub
                                        End If
                                    End If
                                Else
                                    'Lever une erreur
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                    Try
                                        stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                    Catch ey As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                End If
                            End If
                        End If
                End Select
            End If
        Next

        If bad_cookie Then
            Dim result_page As String = "<H1>Erreur 400 - Requête erronée</H1><P>Le cookie du client était invalide, donc il a été réinitialisé vers les paramètres par défaut.<BR><BR>Veuillez retourner à l'<A HREF=""/feed"">index</A> du site.</P>" & vbCrLf

            Dim exp As String = DateTime.UtcNow.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", Globalization.CultureInfo.InvariantCulture)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 400 Bad Request" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Set-Cookie: " & cookie_header & "results=10&size=middle&codec=recent_mpeg1&player=embed&skin=cosmic&resolution=auto&framerate=auto&panel=true&displaycomments=20&vcn=18&trends=enable&displaystream=enable&hidelongvids=yes&vsize=vert1; Path=/; Expires=" & exp & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            WriteLog("Erreur HTTP #400: Le cookie du client était invalide et a été réinitialisé.", ConsoleColor.Yellow, client)
            client.Close()
            Exit Sub
        End If

        'Erreur 414 - URL trop longue
        If Not String.IsNullOrEmpty(request) Then
            If request.Length > 4 Then
                Dim uri_arg As String = Split(request)(1)
                If uri_arg.Length > 512 Then
                    WriteLog("Erreur HTTP #414: URI trop longue.", , client)
                    Dim toolongdata As Byte() = GetHTTPBytes(414, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 414 - URI trop longue</H1>" & vbCrLf & "<P>La requête ne peut pas être traitée, car l'URL spécifiée est trop longue.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                    Try
                        stream.Write(toolongdata, 0, toolongdata.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try

                    client.Close()
                    Exit Sub
                End If
            End If
        End If

        If request.Length > 8192 Then
            WriteLog("Erreur HTTP #413: Contenu trop grand envoyé au serveur.", , client)
            Dim toomuchdata As Byte() = GetHTTPBytes(414, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 413 - Contenu trop grand</H1>" & vbCrLf & "<P>Trop de données communiquées au serveur. Veuillez envoyer moins d'informations.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

            Try
                stream.Write(toomuchdata, 0, toomuchdata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
            Exit Sub
        ElseIf String.IsNullOrEmpty(request) Then
            'Requête vide
            WriteLog("Erreur HTTP #400: Requête vide envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>La requête HTTP était vide, et ne peut être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
            Exit Sub
        ElseIf request.StartsWith("GET /watch?v=") Or request.StartsWith("GET /stream?v=") Or request.StartsWith("GET /short?v=") Then
            'Demande de lecture d'une vidéo par le client
            Dim watcharg As String = Split(request)(1)
            Dim req As String = String.Empty

            If request.StartsWith("GET /watch?v=") Then
                watcharg = watcharg.Remove(0, 9)
                vt = RequestVideoType.WatchVideo
            ElseIf request.StartsWith("GET /short?v=") Then
                watcharg = watcharg.Remove(0, 9)
                vt = RequestVideoType.ShortVideo

                If used_codec = "avi_msvideo1" Or used_codec = "avi_cinepak" Or used_codec = "mov_cinepak" Or used_codec = "mpeg1" Then
                    Dim baddata As Byte() = GetHTTPBytes(503, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 503 - Service indisponible</H1>" & vbCrLf & "<P>La lecture des vidéos dites ""short"", qui sont au format vertical, est indisponible avec le codec vidéo <I>'" & used_codec & "'</I>.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/config.cgi"">ici</A> pour naviguer sur le panneau de configuration, et changer de codec vidéo.</P>" & vbCrLf)

                    Try
                        stream.Write(baddata, 0, baddata.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try

                    client.Close()
                    Exit Sub
                End If

            Else
                watcharg = watcharg.Remove(0, 10)
                vt = RequestVideoType.StreamVideo
            End If

            UpdateCache()

            Dim current_list As String = String.Empty

            If using_vlc Then
                used_codec = "mp4"
                used_resolution = "480p"
            End If

            If watcharg.Contains("&") Then
                Dim fp As String = watcharg.Remove(0, watcharg.IndexOf("?") + 1)

                If Not String.IsNullOrEmpty(fp) AndAlso fp.Length > 0 Then
                    If fp.Contains("=") Then
                        Dim get_params As New List(Of String)
                        If fp.Contains("&") Then
                            get_params.AddRange(fp.Split("&"))
                        Else
                            get_params.Add(fp)
                        End If

                        For Each gp As String In get_params
                            If gp.Contains("=") Then
                                Dim single_params() = Split(gp, "=")

                                For i As Integer = 0 To &H1F
                                    single_params(0) = single_params(0).Replace(Chr(i), String.Empty)
                                    single_params(1) = single_params(1).Replace(Chr(i), String.Empty)
                                Next

                                single_params(0) = LCase(single_params(0))
                                'single_params(1) = LCase(single_params(1))

                                If single_params(0).Length < 100 AndAlso single_params(1).Length < 100 Then
                                    Select Case single_params(0)
                                        Case "codec"
                                            If list_usedcodec.Contains(single_params(1)) Then used_codec = single_params(1)
                                        Case "resolution"
                                            If list_resolution.Contains(single_params(1)) Then used_resolution = single_params(1)
                                        Case "framerate"
                                            If list_framerate.Contains(single_params(1)) Then frame_rate = single_params(1)
                                        Case "player"
                                            If list_used_player.Contains(single_params(1)) Then used_player = single_params(1)
                                        Case "size"
                                            If list_playersize.Contains(single_params(1)) Then player_size = single_params(1)
                                        Case "vsize"
                                            If list_vsize.Contains(single_params(1)) Then player_vsize = single_params(1)
                                        Case "lastsearch"
                                            req = Uri.UnescapeDataString(single_params(1))
                                            req = CleanText(req)
                                        Case "list"
                                            If single_params(1).StartsWith("PL") Or single_params(1).StartsWith("UC") Then
                                                current_list = single_params(1)
                                            End If
                                    End Select
                                End If
                            End If
                        Next
                    End If
                End If
            End If

            Dim num_frame_rate As Integer = 24
            Dim num_used_resolution As Integer = 240

            'Obtenir le cookie du client
            If frame_rate = "auto" Then
                Select Case used_codec
                    Case "mpeg1", "avi_msvideo1", "rm", "avi_yuv", "mov_rpza", "avi_mjpeg", "mov_mjpeg" : num_frame_rate = 15
                    Case "3gp", "avi_cinepak", "mov_cinepak" : num_frame_rate = 10
                    Case "wmv2", "mp4", "mov_svq1", "avi_mpeg4", "wmv1", "mov_mpeg4", "xvid" : num_frame_rate = 25
                    Case "flv", "legacy_mp4", "recent_mpeg1" : num_frame_rate = 24
                    Case Else : num_frame_rate = 25
                End Select
            Else
                num_frame_rate = CInt(frame_rate)
            End If

            If used_resolution = "auto" Then
                Select Case used_codec
                    Case "avi_mpeg4", "wmv2", "mov_svq1", "flv", "wmv1", "mov_mpeg4", "xvid", "legacy_mp4" : num_used_resolution = 480
                    Case "avi_msvideo1", "mpeg1", "avi_yuv", "mov_rpza", "avi_mjpeg", "mov_mjpeg", "recent_mpeg1" : num_used_resolution = 240
                    Case "rm", "3gp", "mov_cinepak", "avi_cinepak" : num_used_resolution = 144
                    Case "mp4" : num_used_resolution = 720
                    Case Else : num_used_resolution = 360
                End Select
            Else
                num_used_resolution = CInt(used_resolution.Replace("p", String.Empty))
            End If

            If used_codec = "avi_msvideo1" Then
                If num_used_resolution > 480 Then
                    num_used_resolution = 480
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "rm" Or used_codec = "mov_rpza" Or used_codec = "mov_cinepak" Then
                If num_used_resolution > 360 Then
                    num_used_resolution = 360
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "wmv1" Or used_codec = "mov_svq1" Or used_codec = "recent_mpeg1" Then
                If num_used_resolution > 480 Then
                    num_used_resolution = 480
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "avi_yuv" Or used_codec = "avi_mjpeg" Or used_codec = "mov_mjpeg" Then
                If num_used_resolution > 240 Then
                    num_used_resolution = 240 'Ne pas activer la HD ou SD sur AVI YUV/MJPEG et le MOV MJPEG, pour éviter de produire des fichiers énormes, qui exigeraient beaucoup de ressources.
                End If
                If num_frame_rate >= 30 Then num_frame_rate = 25
            End If

            If used_codec = "avi_cinepak" Then
                If num_used_resolution > 240 Then
                    num_used_resolution = 240
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "mpeg1" Then
                num_used_resolution = 240
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "3gp" Then
                '96p, 120p et 144p uniquement
                If num_used_resolution > 240 Then
                    num_used_resolution = 240
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            'Le WMV2 et le MP4 ne sont pas bridés

            'On retire les paramètres qui suivent "&".
            If watcharg.Contains("&") Then
                watcharg = watcharg.Substring(0, watcharg.IndexOf("&"))
            End If

            Dim output_path As String = Nothing 'Fichier généré
            Dim output_filename As String = Nothing 'Nom du fichier généré, sans le chemin
            Dim tmp_filename As String = String.Empty

            'Pour garantir la compatibilité avec un navigateur fait maison
            If is_mdx Then
                used_codec = "wmv2"
                used_player = "embed"
                num_frame_rate = 24
                num_used_resolution = 240
            End If

            'En fonction du codec/format vidéo demandé, on génère un fichier output_id_000p.ext, où id correspond à l'identifiant de la vidéo YouTube voulue, "000" à la résolution voulue (p = pixels) et "ext" correspond à l'extension.
            Select Case used_codec
                Case "mpeg1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.mpg"
                Case "recent_mpeg1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_recent.mpg"
                Case "avi_mpeg4" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mpeg4.avi"
                Case "avi_yuv" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_yuv.avi"
                Case "avi_cinepak" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_cinepak.avi"
                Case "avi_mjpeg" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mjpeg.avi"
                Case "xvid" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_xvid.avi"
                Case "3gp" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.3gp"
                Case "avi_msvideo1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_msvideo1.avi"
                Case "rm" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.rm"
                Case "mp4"
                    If num_used_resolution = 96 Then num_used_resolution = 144 'Forcer le 144p, pour garantir une cohérence entre les résolutions YouTube et du serveur au format MP4.
                    If num_used_resolution = 120 Then num_used_resolution = 144
                    tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.mp4"
                Case "legacy_mp4"
                    If num_used_resolution = 96 Then num_used_resolution = 144 'Forcer le 144p, pour garantir une cohérence entre les résolutions YouTube et du serveur au format MP4.
                    If num_used_resolution = 120 Then num_used_resolution = 144
                    tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_legacy.mp4"
                Case "wmv2" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_wmv2.wmv"
                Case "wmv1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_wmv1.wmv"
                Case "mov_cinepak" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_cinepak.mov"
                Case "mov_svq1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_svq1.mov"
                Case "mov_mpeg4" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mpeg4.mov"
                Case "mov_rpza" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_rpza.mov"
                Case "mov_mjpeg" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mjpeg.mov"
                Case "flv" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.flv"
                Case Else
                    'Fallback vers MPEG-4
                    num_used_resolution = 240
                    tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mpeg4.avi"
            End Select

            Dim a As Integer = tmp_filename.IndexOf("."c)
            Dim destfile As String = String.Empty
            output_filename = GetShortName(tmp_filename) & UCase(tmp_filename.Substring(a, tmp_filename.Length - a))
            output_path = CurDir() & "\vidcache\" & output_filename

            'Début du traitement de la requête. On vérifie si l'ID est valide (pas s'il existe).
            If LooksLikeYoutubeID(watcharg) Then
                Dim last_view As String = String.Empty
                last_view = watcharg

                If vt = RequestVideoType.ShortVideo Then
                    WriteLog("Vidéo demandée: https://www.youtube.com/shorts/" & last_view & " (Maximum 1080p60)", ConsoleColor.Green, client)
                Else
                    WriteLog("Vidéo demandée: https://www.youtube.com/watch?v=" & last_view & " (Maximum 1080p60)", ConsoleColor.Green, client)
                End If

                If IsNetworkAvailable() Then
                    'Mise en cache du titre (et de l'ID)
                    Dim tmp_prop As VideoProperties = GetVideo(watcharg)

                    If (tmp_prop.Duration > 10080) OrElse (tmp_prop.Duration > 3600 And forbid_long_vids) Then
                        Dim baddata As Byte() = GetHTTPBytes(503, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 503 - Service indisponible</H1>" & vbCrLf & "<P>La vidéo est trop longue pour être traitée par le serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                        Try
                            stream.Write(baddata, 0, baddata.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        End Try

                        client.Close()
                        Exit Sub
                    End If

                    'Si la vidéo n'est pas en cache, le logiciel va interroger yt-dlp pour l'obtenir.

                    Dim found_video As Boolean = False

                    For Each seek_file As String In IO.Directory.GetFiles(CurDir() & "\srccache")
                        seek_file = seek_file.Remove(0, Convert.ToString(CurDir() & "\srccache").Length + 1)
                        If LCase(seek_file).Contains(LCase(GetMD5(last_view))) Then
                            found_video = True 'Balayer le dossier pour trouver le fichier voulu
                        End If
                    Next

                    If Not found_video Then
                        'Exécution du processus d'obtention de la vidéo souhaitée.
                        WriteLog("Téléchargement de la vidéo en cours...", ConsoleColor.Green, client)

                        Dim freeSpace As Long = -1
                        For Each c As IO.DriveInfo In IO.DriveInfo.GetDrives()
                            If LCase(CurDir()).StartsWith(LCase(c.RootDirectory.ToString)) Then
                                freeSpace = c.AvailableFreeSpace
                                Exit For
                            End If
                        Next

                        If freeSpace >= 0 And freeSpace <= 134217728 Then
                            Dim baddata As Byte() = GetHTTPBytes(507, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 507 - Espace disque insuffisant</H1>" & vbCrLf & "<P>Il n'y a plus assez d'espace sur le périphérique de stockage du serveur pour mettre en cache la vidéo demandée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(baddata, 0, baddata.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        Else
                            'Formatage du nom de fichier de destination vers un nom insensible à la casse (usage de l'algorithme MD5) -> ID YouTube vers hash MD5 + extension .dat, qui contiendra MP4 H.264, WebM VP8, VP9, AV1, etc.
                            destfile = CurDir() & "\srccache\" & GetMD5(last_view)

                            If Not IO.File.Exists(destfile) Then
                                'La commande suivante demande une vidéo au format MP4 (Codec vidéo H.264, audio M4A).
                                Dim lock_file_download As String = CurDir() & "\prclocks\download_" & GetMD5(last_view) & ".lock"

                                If IO.File.Exists(lock_file_download) Then
                                    Dim ise_data As Byte() = GetHTTPBytes(409, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 409 - Demande en conflit</H1>" & vbCrLf & "<P>La vidéo demandée est déjà en cours de téléchargement par le serveur. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                    Try
                                        stream.Write(ise_data, 0, ise_data.Length)
                                    Catch ex As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                Else
                                    'La vidéo est téléchargée en forçant le 1080p

                                    If number_of_dls >= 10 Then
                                        Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes en cours</H1>" & vbCrLf & "<P>Il y a déjà 10 vidéos en cours de téléchargement. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                        Try
                                            stream.Write(ise_data, 0, ise_data.Length)
                                        Catch ex As Exception
                                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                        End Try

                                        client.Close()
                                        Exit Sub
                                    End If

                                    'Démarrage du processus de téléchargement, et contrôle du processus via un fichier de verrouillage (en cas de fermeture ou plantage)
                                    Dim has_err As Boolean = False

                                    number_of_dls += 1

                                    Dim op As OutputResponse = Nothing

                                    If vt = RequestVideoType.ShortVideo Then
                                        op = LaunchProcess("-f ""bv*[height<=1080][fps<=60]+ba/b[height<=1080][fps<=60]"" --no-part --no-continue -o """ & destfile & """ ""https://www.youtube.com/shorts/" & last_view & """", , lock_file_download, last_view)
                                    Else
                                        op = LaunchProcess("-f ""bv*[height<=1080][fps<=60]+ba/b[height<=1080][fps<=60]"" --no-part --no-continue -o """ & destfile & """ ""https://www.youtube.com/watch?v=" & last_view & """", , lock_file_download, last_view)
                                    End If

                                    If Not String.IsNullOrEmpty(op.OutputData) AndAlso op.OutputData.Length > 0 Then
                                        WriteLog(op.OutputData, ConsoleColor.Cyan, client)
                                    End If

                                    If Not String.IsNullOrEmpty(op.ErrorData) AndAlso op.ErrorData.Length > 0 Then
                                        WriteLog(op.ErrorData, ConsoleColor.Red, client)
                                    End If

                                    If op.HasErrors Then
                                        WriteLog("Erreur lors du processus de téléchargement de la vidéo: " & op.ExceptionMessage, ConsoleColor.Red)
                                        has_err = True
                                    End If

                                    If has_err Then
                                        Dim ise_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le traitement de la vidéo demandée n'a pas pu être effectué (Identifiant connu: <I>" & last_view & "</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                        Try
                                            stream.Write(ise_data, 0, ise_data.Length)
                                        Catch ex As Exception
                                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                        End Try

                                        client.Close()
                                        Exit Sub
                                    End If

                                    number_of_dls -= 1
                                End If
                            Else
                                WriteLog("La vidéo a déjà été téléchargée, et est disponible en cache.")
                            End If

                            'Trouver le fichier généré depuis le nom
                            For Each source_f As String In IO.Directory.GetFiles(CurDir() & "\srccache")
                                If source_f.Contains(GetMD5(last_view)) Then
                                    destfile = source_f
                                    Exit For
                                End If
                            Next

                            If Not IO.File.Exists(destfile) Then
                                Dim ise_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La vidéo demandée n'a pas pu être téléchargée (Identifiant connu: <I>" & last_view & "</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                Try
                                    stream.Write(ise_data, 0, ise_data.Length)
                                Catch ex As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                End Try

                                client.Close()
                                Exit Sub
                            End If
                        End If
                    Else
                        WriteLog("Vidéo déjà présente dans le cache source ! Traitement en cours de la vidéo...")
                    End If

                    'Trouver le fichier généré depuis le nom
                    For Each source_f As String In IO.Directory.GetFiles(CurDir() & "\srccache")
                        If source_f.Contains(GetMD5(last_view)) Then
                            destfile = source_f
                            Exit For
                        End If
                    Next

                    If Not IO.File.Exists(output_path) Then
                        If number_of_vids >= 10 Then
                            Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes en cours</H1>" & vbCrLf & "<P>Il y a déjà 10 vidéos en cours de traitement. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(ise_data, 0, ise_data.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        End If

                        Dim op_conv_video As OutputResponse = Nothing
                        Dim lock_file_output As String = CurDir() & "\prclocks\output_" & GetMD5(output_path) & ".lock"
                        number_of_vids += 1

                        If IO.File.Exists(lock_file_output) Then
                            'Si le fichier est déjà en cours de conversion
                            Dim ise_data As Byte() = GetHTTPBytes(409, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 409 - Demande en conflit</H1>" & vbCrLf & "<P>La vidéo demandée est déjà en cours de conversion par le serveur. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(ise_data, 0, ise_data.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            number_of_vids -= 1
                            client.Close()
                            Exit Sub
                        End If

                        Select Case used_codec
                            Case "mpeg1"
                                'Codec vidéo MPEG-1, audio MP2 (100% compatible)
                                num_used_resolution = 240
                                WriteLog("Conversion du fichier vidéo vers le format MPEG (Configuration 100% compatible)...")
                                WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=352:240 -r 30000/1001 -c:v mpeg1video -b:v 1150k -maxrate 1150k -minrate 1150k -bufsize 327680 -c:a mp2 -b:a 96k -ar 44100 -ac 2 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "recent_mpeg1"
                                'Codec vidéo MPEG-1, audio MP2
                                WriteLog("Conversion du fichier vidéo vers le format MPEG (Codec vidéo MPEG-1, codec audio MP2)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -c:v mpeg1video -vf scale=-2:" & used_resolution & " -bufsize 442k -maxrate 50000k -q:v 7 -c:a mp2 -b:a 192k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "avi_mpeg4"
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Codec vidéo MPEG-4, codec audio MP3)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                'Format AVI encodé avec MPEG-4 (codec vidéo assez fonctionnel et compatible avec les systèmes Windows), et MP3.
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "avi_yuv"
                                'Format AVI YUV (sans codec) avec PCM
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Vidéo YUV, codec audio PCM)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v rawvideo -pix_fmt yuyv422 -vtag YUY2 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "wmv2"
                                WriteLog("Conversion du fichier vidéo vers le format WMV [Nouveau] (Codec vidéo WMV2, codec audio WMAv2)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                'Format WMV, très utilisé sous Windows, depuis Windows 98. Codec WMV2 et WMAv2
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv2 -b:v 800k -c:a wmav2 -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "wmv1"
                                'Format WMV ancien, codec WMV2, audio WMAv1.
                                WriteLog("Conversion du fichier vidéo vers le format WMV [Ancien] (Codec vidéo WMV1, codec audio WMAv1)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv1 -b:v 500k -c:a wmav1 -b:a 128k -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "rm"
                                WriteLog("Conversion du fichier vidéo vers le format RealMedia (Codec vidéo RV10, codec audio AC3)...")
                                'Format Real Media (code par Le Jarb aidé de Léo AI). A permis de faire fonctionner la lecture intégrée sous IE 3.0 et Windows 3.11.
                                'Codec vidéo RV10 et audio AC3
                                If vt = RequestVideoType.ShortVideo Then
                                    'Une seule résolution, la seule réellement compatible 9:16 et qui soit multiple de 16, la plus "époque", les autres résolutions trouvées déformaient le ratio.
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=144:256 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                Else
                                    If num_used_resolution <= 120 Then
                                        WriteLog("Résolution 120p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=160:128 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    ElseIf num_used_resolution = 144 Then
                                        WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=256:144 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    ElseIf num_used_resolution = 240 Then
                                        WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=320:240 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    Else
                                        WriteLog("Résolution 360p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=480:360 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    End If
                                End If
                            Case "3gp"
                                'Format 3GP (pour les vieux mobiles Nokia, SONY, etc.), codec vidéo H.263, audio AMR-NB
                                WriteLog("Conversion du fichier vidéo vers le format 3GP (Codec vidéo H.263, codec audio AMR-NB)...")
                                Select Case num_used_resolution
                                    Case 96
                                        WriteLog("Résolution 96p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=128:96 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    Case 120, 144
                                        WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=176:144 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                    Case Else '240p compris
                                        WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                        op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=352:288 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                End Select
                            Case "mov_cinepak"
                                WriteLog("Conversion du fichier vidéo vers le format Apple QuickTime (Codec vidéo Cinepak, codec audio PCM)...")
                                'Format QuickTime (codec vidéo Cinepak, fortement utilisé dans les années 1990, et PCM pour l'audio)
                                If num_used_resolution <= 120 Then
                                    WriteLog("Résolution 120p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 144 Then
                                    WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                Else
                                    WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                End If
                            Case "mov_svq1"
                                WriteLog("Conversion du fichier vidéo vers le format Apple QuickTime (Codec vidéo Sorenson SVQ1, codec audio PCM)...")
                                'Format QuickTime (codec vidéo Sorenson SVQ1, surtout utilisé dans les années 2000, et codec audio MP3)
                                If num_used_resolution >= 720 Then num_used_resolution = 480 'HQ indisponible
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v svq1 -q:v 3 -c:a libmp3lame -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "mov_mpeg4"
                                'Format QuickTime (codec vidéo MPEG-4, audio MP3)
                                If num_used_resolution >= 720 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo vers le format Apple QuickTime (Codec vidéo MPEG-4, codec audio MP3)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mpeg4 -b:v 500k -c:a libmp3lame -b:a 128k -ar 44100 -ac 2 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "mov_mjpeg"
                                'Format QuickTime, encodé MJPEG et PCM
                                If num_used_resolution > 480 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo vers le format Apple QuickTime (Codec vidéo MJPEG, codec audio PCM)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mjpeg -q:v 4 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "mov_rpza"
                                WriteLog("Conversion du fichier vidéo vers le format Apple QuickTime (Codec vidéo RPZA, codec audio PCM)...")
                                'Format QuickTime (codec vidéo RPZA, format très Apple des années 1990, et PCM pour l'audio)
                                If num_used_resolution <= 120 Then
                                    WriteLog("Résolution 120p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 144 Then
                                    WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                Else
                                    WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                End If
                            Case "avi_mjpeg"
                                'Format AVI encodé avec MJPEG et PCM
                                If num_used_resolution > 480 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Codec vidéo MJPEG, codec audio PCM)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mjpeg -q:v 4 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "avi_msvideo1"
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Codec vidéo MSVideo1, codec audio PCM)...")
                                'Format AVI encodé avec Microsoft Video 1 (fonctionne en pratique sous toutes les versions de Windows, y compris Windows 3.11, surtout puisqu'il accompagné du codec audio PCM).
                                If num_used_resolution <= 120 Then
                                    WriteLog("Résolution 120p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 144 Then
                                    WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 240 Then
                                    WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 360 Then
                                    WriteLog("Résolution 360p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=480:360 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                Else
                                    WriteLog("Résolution 480p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=640:480 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                End If
                            Case "avi_cinepak"
                                'Cinepak version AVI, audio PCM
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Codec vidéo Cinepak, codec audio PCM)...")
                                'Format AVI encodé avec Cinepak (codec répandu dans les années 90, et pris en charge par Windows 3.11, surtout accompagné du codec audio PCM).
                                If num_used_resolution <= 120 Then
                                    WriteLog("Résolution 120p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 144 Then
                                    WriteLog("Résolution 144p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                ElseIf num_used_resolution = 240 Then
                                    WriteLog("Résolution 240p @ " & frame_rate.ToString & " utilisée.")
                                    op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                                End If
                            Case "mp4"
                                'Format MP4 - Codec vidéo: H.264, codec audio: AAC, avec le format pixel forcé à YUV420P pour éviter les erreurs d'affichage sur les vieux lecteurs. Baseline et level 3.0 avec pour rendre compatible avec les vieux lecteurs Android.
                                WriteLog("Conversion du fichier vidéo vers le format MP4 (Codec vidéo H.264, codec audio AAC)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v libx264 -preset fast -crf 23 -pix_fmt yuv420p -movflags +faststart -c:a aac -b:a 192k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "legacy_mp4"
                                'Format MP4 - Adapté aux anciens Android
                                WriteLog("Conversion du fichier vidéo vers le format MP4 (Profil adapté aux vieux lecteurs)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                Dim target_height As Integer = num_used_resolution

                                'Calcul largeur 4:3 finale
                                Dim target_width As Integer = CInt((target_height / 3) * 4)

                                'Hauteur réelle vidéo (16:9 conservé)
                                Dim inner_height As Integer = CInt(target_height * 0.75)

                                'Force dimensions paires pour H264
                                If inner_height Mod 2 <> 0 Then inner_height -= 1
                                If target_width Mod 2 <> 0 Then target_width -= 1
                                If target_height Mod 2 <> 0 Then target_height -= 1

                                Dim vf_arg As String = """scale=-2:" & inner_height.ToString & ",pad=" & target_width.ToString & ":" & target_height.ToString & ":(ow-iw)/2:(oh-ih)/2"""
                                op_conv_video = LaunchProcess("-i """ & destfile & """ " & "-vf " & vf_arg & " " & "-r " & num_frame_rate & " -c:v libx264 -preset fast -crf 23 -profile:v baseline -level 3.0 -pix_fmt yuv420p -movflags +faststart -c:a aac -b:a 128k -ar 44100 -ac 2 """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "xvid"
                                'Format Xvid, avec le conteneur AVI, et le codec audio MP3
                                WriteLog("Conversion du fichier vidéo vers le format AVI (Codec vidéo Xvid, codec audio MP3)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v libxvid -qscale:v 3 -vtag xvid -c:a libmp3lame -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case "flv"
                                'Format FLV (Codec vidéo Sorenson Spark, audio MP3) [Macromedia Flash Video]
                                WriteLog("Conversion du fichier vidéo vers le format vidéo Flash (Codec vidéo Sorenson Spark, codec audio MP3)...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v flv -b:v 500k -c:a libmp3lame -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                            Case Else
                                WriteLog("Aucun format de destination valide, usage d'un profil par défaut...")
                                WriteLog("Résolution " & num_used_resolution.ToString & "p @ " & frame_rate.ToString & " utilisée.")
                                'Par défaut, envoyer du MPEG4.
                                op_conv_video = LaunchProcess("-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 128k """ & output_path & """", "ffmpeg.exe", lock_file_output, last_view)
                        End Select

                        'WriteLog("Veuillez NE PAS FERMER la fenêtre.", ConsoleColor.DarkRed)

                        If op_conv_video.HasErrors Then
                            WriteLog("Erreur lors de la conversion de la vidéo: " & op_conv_video.ExceptionMessage, ConsoleColor.Red)
                        End If

                        number_of_vids -= 1
                    Else
                        WriteLog("Fichier vidéo de destination déjà existant au format demandé! Aucune conversion n'est donc nécessaire.", , client)
                    End If

                    'Formatage de la page en HTML, avec lecteur intégré

                    If vt = RequestVideoType.WatchVideo Or vt = RequestVideoType.ShortVideo Then
                        patternpage.Append(InitValues(EscapeHtml(tmp_prop.Title), , wanted_skin, , used_player))

                        Dim media_type As String = "video/mp4"

                        Select Case used_codec
                            Case "mp4", "legacy_mp4" : media_type = "video/mp4"
                            Case "rm" : media_type = "application/vnd.rn-realmedia"
                            Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak", "avi_mjpeg", "xvid" : media_type = "video/x-msvideo"
                            Case "wmv1", "wmv2" : media_type = "video/x-ms-wmv"
                            Case "mov_cinepak", "mov_svq1", "mov_mpeg4", "mov_rpza", "mov_mjpeg" : media_type = "video/quicktime"
                            Case "3gp" : media_type = "video/3gpp"
                            Case "mpeg1", "recent_mpeg1" : media_type = "video/mpeg"
                            Case "flv" : media_type = "video/x-flv"
                            Case Else : media_type = "application/octet-stream"
                        End Select

                        Dim player_width, player_height As Integer
                        Dim player_prop As String = String.Empty
                        player_width = 640 'Failsafe
                        player_height = 480

                        'Détermination de la taille du lecteur via le cookie
                        If vt = RequestVideoType.WatchVideo Then
                            Select Case player_size
                                Case "micro"
                                    'Lecteur microscopique, pour les scénarios d'écrans en très faible résolution (téléphones mobiles)
                                    player_width = 160
                                    player_height = 120
                                Case "ultrasmall"
                                    'Pour les écrans en faible résolution (320x240 par exemple)
                                    player_width = 256
                                    player_height = 192
                                Case "small"
                                    'Petit lecteur, utile pour les écrans standards des années 1980/1990 (QVGA)
                                    player_width = 320
                                    player_height = 240
                                Case "cs"
                                    'Taille classique du lecteur Youtube des années 2000
                                    player_width = 480
                                    player_height = 360
                                Case "middle"
                                    'Moyen lecteur (correspondant au standard VGA)
                                    player_width = 640
                                    player_height = 480
                                Case "large"
                                    'Lecteur large, format pouvant afficher du 16:9 (WVGA)
                                    player_width = 854
                                    player_height = 480
                                Case "cinema"
                                    'Format cinéma, également au 16:9
                                    player_width = 1280
                                    player_height = 720
                                Case "bigcinema"
                                    'Format cinéma grand format, également au 16:9
                                    player_width = 2560
                                    player_height = 1440
                                Case "gold1"
                                    'Format 16:10 standard
                                    player_width = 1280
                                    player_height = 800
                                Case "gold2"
                                    'Format 16:10 grand format
                                    player_width = 1440
                                    player_height = 900
                                Case "aheight"
                                    'Taille renseignée par la résolution elle-même de la vidéo
                                    player_height = num_used_resolution
                                    player_width = num_used_resolution * 4 / 3
                                Case "vertical1"
                                    'Format vertical 9:16 (270x480)
                                    player_width = 270
                                    player_height = 480
                                Case "vertical2"
                                    'Format vertical 9:16 (360x640)
                                    player_width = 360
                                    player_height = 640
                                Case "vertical3"
                                    'Format vertical 9:16 (720x1280)
                                    player_width = 720
                                    player_height = 1280
                                Case "eh"
                                    player_width = 800
                                    player_height = 600
                                Case "ot"
                                    player_width = 1024
                                    player_height = 768
                                Case "otsh"
                                    player_width = 1600
                                    player_height = 1200
                                Case "auto"
                                    'Taille contrôlée avec Javascript
                                    player_width = 640
                                    player_height = 480 'Failsafe

                                    Dim tmp_w, tmp_h As Integer
                                    tmp_w = 640
                                    tmp_h = 480

                                    Dim tmp_dimensions() As String = Split(tmp_prop.Dimensions, ":")
                                    tmp_w = CInt(tmp_dimensions(0))
                                    tmp_h = CInt(tmp_dimensions(1))

                                    'Utilisation du Javascript pour redimensionner de façon dynamique le lecteur intégré.
                                    patternpage.AppendLine("<script language=""javascript"">")
                                    patternpage.AppendLine(" function resizePlayer() {")
                                    patternpage.AppendLine("  var player = document.getElementById(""mainplayer"");")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  var winW = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;")
                                    patternpage.AppendLine("  var winH = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  // Marges")
                                    patternpage.AppendLine("  var maxW = winW - 40;")
                                    patternpage.AppendLine("  var maxH = winH - 120;")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  // Ratio 4:3")
                                    patternpage.AppendLine("  var ratioW = " & tmp_w.ToString & ";")
                                    patternpage.AppendLine("  var ratioH = " & tmp_h.ToString & ";")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  // Calcul basé sur largeur")
                                    patternpage.AppendLine("  var width = maxW;")
                                    patternpage.AppendLine("  var height = Math.floor(width * ratioH / ratioW);")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  // Si ça dépasse en hauteur, alors recalcul de la taille du lecteur")
                                    patternpage.AppendLine("  if (height > maxH) {")
                                    patternpage.AppendLine("   height = maxH;")
                                    patternpage.AppendLine("   width = Math.floor(height * ratioW / ratioH);")
                                    patternpage.AppendLine("  }")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  // Minimum de 240 pixels de large")
                                    patternpage.AppendLine("  if (width < 240) {")
                                    patternpage.AppendLine("   width = 240;")
                                    patternpage.AppendLine("   height = Math.floor(width * ratioH / ratioW);")
                                    patternpage.AppendLine("  }")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  player.width = width;")
                                    patternpage.AppendLine("  player.height = height;")
                                    patternpage.AppendLine(" }")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine(" window.onload = resizePlayer;")
                                    patternpage.AppendLine(" window.onresize = resizePlayer;")
                                    patternpage.AppendLine("</script>")
                                    patternpage.AppendLine()
                                'ChatGPT m'a généré ce code.
                                Case "fulljs"
                                    'Plein écran avec Javascript

                                    If used_player = "video" Then
                                        patternpage.AppendLine("<!DOCTYPE html>")
                                    Else
                                        patternpage.AppendLine("<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">")
                                    End If

                                    patternpage.AppendLine("<HTML>")
                                    patternpage.AppendLine("<HEAD>")
                                    patternpage.AppendLine(" <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">")
                                    patternpage.AppendLine(" <META CHARSET=""iso-8859-1"" />")
                                    patternpage.AppendLine(" <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />")
                                    patternpage.AppendLine("</HEAD>")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" ALINK=""#C2272F"" VLINK=""#C2272F"" STYLE=""display: block; padding: 0px 0px 0px 0px; margin: 0px 0px 0px 0px;"" TOPMARGIN=0 LEFTMARGIN=0 MARGINHEIGHT=0 MARGINWIDTH=0>")
                                    patternpage.AppendLine("<script language=""javascript"">")
                                    patternpage.AppendLine(" function resizePlayer() {")
                                    patternpage.AppendLine("  var player = document.getElementById(""mainplayer"");")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  var winW = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;")
                                    patternpage.AppendLine("  var winH = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("  player.width = winW;")
                                    patternpage.AppendLine("  player.height = winH;")
                                    patternpage.AppendLine(" }")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine(" window.onload = resizePlayer;")
                                    patternpage.AppendLine(" window.onresize = resizePlayer;")
                                    patternpage.AppendLine("</script>")
                                    patternpage.AppendLine()
                                    player_prop = "%"
                                'Code de ChatGPT modifié.
                                Case "fullscreen"
                                    'Plein écran avec HTML (peut dépasser le cadre)
                                    link_color = "#c2272f"

                                    If used_player = "video" Then
                                        patternpage.AppendLine("<!DOCTYPE html>")
                                    Else
                                        patternpage.AppendLine("<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">")
                                    End If

                                    patternpage.AppendLine("<HTML>")
                                    patternpage.AppendLine("<HEAD>")
                                    patternpage.AppendLine(" <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">")
                                    patternpage.AppendLine(" <META CHARSET=""iso-8859-1"" />")
                                    patternpage.AppendLine(" <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />")
                                    patternpage.AppendLine("</HEAD>")
                                    patternpage.AppendLine()

                                    patternpage.AppendLine("<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" ALINK=""#C2272F"" VLINK=""#C2272F"" STYLE=""display: block; padding: 0px 0px 0px 0px; margin: 0px 0px 0px 0px;"" TOPMARGIN=0 LEFTMARGIN=0 MARGINHEIGHT=0 MARGINWIDTH=0>")

                                    player_width = 100
                                    player_height = 100
                                    player_prop = "%"
                            End Select
                        Else
                            Select Case player_vsize
                                Case "vert0"
                                    player_width = 144
                                    player_height = 256
                                Case "vert1"
                                    player_width = 270
                                    player_height = 480
                                Case "vert2"
                                    player_width = 360
                                    player_height = 640
                                Case "vert3"
                                    player_width = 720
                                    player_height = 1280
                                Case Else
                                    player_width = 270
                                    player_height = 480
                            End Select
                        End If

                        'Marge pour les contrôles
                        If used_player <> "video" Then player_height += 20

                        'Si aucun argument mot-clef n'est spécifié, les vidéos relatives regardent le titre de la vidéo
                        If String.IsNullOrEmpty(req) OrElse req.Length = 0 Then req = tmp_prop.Title

                        ''Titre de la vidéo dans la page
                        'Dim actual_width As String = "640"

                        'If player_prop = "%" Then
                        '    actual_width = "100%"
                        'Else
                        '    actual_width = Convert.ToString(Math.Max(480, player_width) + IIf(right_panel, 400, 0))
                        'End If

                        If player_size <> "fulljs" AndAlso player_size <> "fullscreen" Then
                            patternpage.AppendLine("<CENTER><TABLE BORDER=0 CELLSPACING=0 CELLPADDING=4 ALIGN=CENTER VALIGN=TOP") 'WIDTH=" & actual_width & ">"
                            patternpage.AppendLine(" <TR>")
                            patternpage.AppendLine("  <TD COLSPAN=2>")
                            If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine("<CENTER>")
                            patternpage.Append("   <H2 STYLE=""line-height: 24px;""><B>" & EscapeHtml(tmp_prop.Title) & IIf(vt = RequestVideoType.ShortVideo, " par <A HREF=""" & tmp_prop.Channel_URL.Replace("section=videos", "section=shorts") & """>" & tmp_prop.Creator & "</A>", String.Empty))
                            If display_stream_button And vt = RequestVideoType.WatchVideo Then patternpage.Append(" (<A HREF=""/v/" & output_filename & """>Flux&nbsp;direct</A>)")
                            patternpage.AppendLine("</B></H2>")
                            patternpage.AppendLine("  </TD>")
                            If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine("</CENTER>")
                            patternpage.AppendLine(" </TR>")

                            patternpage.AppendLine(" <TR VALIGN=TOP>")

                            If used_player = "no_integration" Then
                                patternpage.AppendLine("  <TD WIDTH=700>")
                            Else
                                If vt = RequestVideoType.ShortVideo Then
                                    Select Case player_vsize
                                        Case "vert0"
                                            patternpage.AppendLine("  <TD WIDTH=300><BR><BR>")
                                        Case "vert1"
                                            patternpage.AppendLine("  <TD WIDTH=300>")
                                        Case "vert2"
                                            patternpage.AppendLine("  <TD WIDTH=400>")
                                        Case "vert3"
                                            patternpage.AppendLine("  <TD WIDTH=800>")
                                    End Select
                                Else
                                    patternpage.AppendLine("  <TD WIDTH=""" & player_width.ToString & player_prop & """>")
                                End If
                            End If

                            patternpage.AppendLine()
                        End If

                        'Le lecteur intégré
                        Select Case used_player
                            Case "legacy_wmp"
                                'Ancien lecteur Windows Media (6.4) intégré avec la balise <object> (ActiveX).
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour Windows Media Player 6.4 -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ CLASSID=""CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95"">")
                                patternpage.AppendLine(" <PARAM NAME=""FileName"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""AutoStart"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""EnableFullScreenControls"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""VideoBorder3D"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""StretchToFit"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""ShowControls"" VALUE=""true"">")
                                If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true""><SCRIPT LANGUAGE=""javascript"">mainplayer.settings.setMode(""loop"", true);</SCRIPT>")
                                patternpage.AppendLine(" <PARAM NAME=""DisplaySize"" VALUE=4>")
                                patternpage.AppendLine(" <PARAM NAME=""DefaultFrame"" VALUE=""" & GetHost() & "getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """>")
                                patternpage.AppendLine("</OBJECT>")
                            Case "wmp"
                                'Nouveau lecteur Windows Media (7.0 et +) intégré avec la balise <object> (ActiveX).
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour Windows Media Player 7.0 et plus -->")
                                patternpage.AppendLine()

                                If player_prop = "%" Then
                                    patternpage.AppendLine("<OBJECT ID=""mainplayer"" WIDTH=""" & player_width.ToString & "%"" HEIGHT=""" & player_height.ToString & "%"" CLASSID=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">")
                                Else
                                    patternpage.AppendLine("<OBJECT ID=""mainplayer"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """ CLASSID=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">")
                                End If

                                patternpage.AppendLine(" <PARAM NAME=""URL"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""AutoStart"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""EnableFullScreenControls"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""VideoBorder3D"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""StretchToFit"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""ShowControls"" VALUE=""true"">")
                                If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true""><SCRIPT LANGUAGE=""javascript"">mainplayer.settings.setMode(""loop"", true);</SCRIPT>")
                                patternpage.AppendLine(" <PARAM NAME=""DefaultFrame"" VALUE=""" & GetHost() & "getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """>")
                                patternpage.AppendLine("</OBJECT>")
                            Case "vlc"
                                'Lecteur VLC Media Player (via ActiveX)
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour le lecteur VLC -->")
                                patternpage.AppendLine()

                                If player_prop = "%" Then
                                    patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""CLSID:9BE31822-FDAD-461B-AD51-BE1D1C159921"" WIDTH=""" & player_width.ToString & "%"" HEIGHT=""" & player_height.ToString & "%"">")
                                Else
                                    patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""CLSID:9BE31822-FDAD-461B-AD51-BE1D1C159921"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """>")
                                End If

                                patternpage.AppendLine(" <PARAM NAME=""target"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""MRL"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""autoplay"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""controller"" VALUE=""true"">") 'Affichage des contrôles du lecteur

                                If vt = RequestVideoType.ShortVideo Then
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true"">")
                                Else
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""false"">")
                                End If

                                patternpage.AppendLine("</OBJECT>")
                            Case "alt_vlc"
                                'Lecteur VLC Media Player (via ActiveX aussi)
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour le lecteur VLC avec un identificateur de classe alternatif -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""CLSID:E23FE9C6-778E-49D4-B537-38FCDE4887D8"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """>")
                                patternpage.AppendLine(" <PARAM NAME=""target"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""MRL"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""autoplay"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""controller"" VALUE=""true"">") 'Affichage des contrôles du lecteur

                                If vt = RequestVideoType.ShortVideo Then
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true"">")
                                Else
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""false"">")
                                End If

                                patternpage.AppendLine("</OBJECT>")
                            Case "embed_vlc"
                                'Lecteur VLC via la balise HTML embed.
                                patternpage.AppendLine("<!-- Embarcation du plugin VLC -->")
                                patternpage.AppendLine()
                                patternpage.AppendLine("<EMBED ID=""mainplayer"" TYPE=""application/x-vlc-plugin"" SRC=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ AUTPLAY=""true"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ />")
                            Case "quicktime"
                                'Lecteur QuickTime via ActiveX (Exclusivement sous Windows)
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour le lecteur Apple QuickTime. -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""CLSID:02BF25D5-8C17-4B23-BC80-D3488ABDDC6B"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """>")
                                patternpage.AppendLine(" <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                patternpage.AppendLine(" <PARAM NAME=""autoplay"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""controller"" VALUE=""true"">")

                                If vt = RequestVideoType.ShortVideo Then
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true"">")
                                Else
                                    patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""false"">")
                                End If

                                patternpage.AppendLine("</OBJECT>")
                                patternpage.AppendLine()
                            Case "embed_quicktime"
                                'Lecteur QuickTime via la balise HTML embed (surtout pour les systèmes Apple)
                                patternpage.AppendLine("<!-- Embarcation d'un lecteur Apple QuickTime -->")
                                patternpage.AppendLine()
                                patternpage.AppendLine("<EMBED ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ CONTROLLER=""true"" AUTOPLAY=""true"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ />")
                            Case "embed"
                                'Balise <embed> générique, une syntaxe et un fonctionnement lancés par NetScape en 1995.
                                patternpage.AppendLine("<!-- Embarcation du contenu multimédia avec la balise HTML embed. -->")
                                patternpage.AppendLine()

                                If used_codec = "rm" Then
                                    If player_prop = "%" Then player_height = 90
                                    patternpage.AppendLine("<EMBED ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ TYPE=""audio/x-pn-realaudio-plugin"" AUTOSTART=""true"" CONTROLS=""ImageWindow"" CONSOLE=""rmplayer"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ /><BR>")
                                    patternpage.AppendLine("<EMBED WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""20"" TYPE=""audio/x-pn-realaudio-plugin"" CONTROLS=""PositionSlider"" CONSOLE=""rmplayer"" />")
                                Else
                                    patternpage.AppendLine("<EMBED ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ AUTOSTART=""true"" />")
                                End If
                            Case "video"
                                'Balise <video> de HTML 5.0 (Standard W3C natif aux navigateurs récents)
                                patternpage.AppendLine("<!-- Utilisation de la balise video de HTML5 -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<video id=""mainplayer"" controls width=""" & player_width.ToString & player_prop & """ height=""" & player_height.ToString & player_prop & """ preload=""auto""" & IIf(vt = RequestVideoType.ShortVideo, " loop", String.Empty) & " autoplay=""true"" poster=""" & GetHost() & "getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """>")
                                patternpage.AppendLine(" <source src=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ />")
                                patternpage.AppendLine(" <P ALIGN=CENTER>Votre navigateur ne semble pas prendre en charge la balise video de HTML5.<BR><BR>Vous pouvez cliquer sur <A HREF=""/config.cgi"">ce lien</A> pour adapter les paramètres de RetroYT à votre configuration.</P>")
                                patternpage.AppendLine("</video>")
                            Case "alt_video"
                                'Balise <video> de HTML 5.0 (Pour les plateformes Nintendo, SONY et Android)
                                patternpage.AppendLine("<!-- Utilisation de la balise video de HTML5 (version adaptée pour les vieilles versions d'Android et les consoles de salon connectées -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<video id=""mainplayer"" webkit-playsinline playsinline controls width=""" & player_width.ToString & player_prop & """ height=""" & player_height.ToString & player_prop & """ preload=""auto""" & IIf(vt = RequestVideoType.ShortVideo, " loop", String.Empty) & " autoplay=""true"" onClick=""this.play();"" poster=""" & GetHost() & "getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """>")
                                patternpage.AppendLine(" <source src=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ />")
                                patternpage.AppendLine(" <P ALIGN=CENTER>Votre navigateur ne semble pas prendre en charge la balise video de HTML5.<BR><BR>Vous pouvez cliquer sur <A HREF=""/config.cgi"">ce lien</A> pour adapter les paramètres de RetroYT à votre configuration.</P>")
                                patternpage.AppendLine("</video>")
                            Case "realplayer"
                                'Intégration du lecteur Real Player (Le code ci-dessous a été créé par Le Jarb, qui s'est appuyé sur Léo AI. Merci pour son implémentation réussie du plugin Real Player, rendant la lecture intégrée sur navigateur possible sous Windows 3.11/NT 3.51)
                                patternpage.AppendLine("<!-- Embarcation du lecteur Real Player 5.0 -->")
                                patternpage.AppendLine()

                                If player_prop = "%" Then player_height = 90
                                patternpage.AppendLine("<EMBED ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ TYPE=""audio/x-pn-realaudio-plugin"" AUTOSTART=""true"" CONTROLS=""ImageWindow"" CONSOLE=""rmplayer"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ /><BR>")
                                patternpage.AppendLine("<EMBED WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""20"" TYPE=""audio/x-pn-realaudio-plugin"" CONTROLS=""PositionSlider"" CONSOLE=""rmplayer"" />")
                            Case "activex_realplayer"
                                'Real Player (ActiveX)
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour Real Player 5.0 -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""CLSID:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """>")
                                patternpage.AppendLine(" <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>")
                                If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true"">")
                                patternpage.AppendLine("</OBJECT>")
                                patternpage.AppendLine()
                            Case "no_integration"
                                'Aucune intégration, donc aucun lecteur affiché. Code HTML bidon qui suit.
                                patternpage.AppendLine("<!-- Aucune intégration activée -->")

                                If vt = RequestVideoType.ShortVideo Then
                                    patternpage.AppendLine("<CENTER><A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""picshort.jpg"" ALT=""Démarrer le short en mode streaming"" STYLE=""border-radius: 4px;"" BORDER=0 /></A></CENTER><BR><BR>")
                                Else
                                    patternpage.AppendLine("<A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""picplay.jpg"" ALT=""Démarrer la lecture en mode streaming"" BORDER=0 STYLE=""border-radius: 4px;"" /></A><BR><BR>")
                                End If

                                patternpage.AppendLine()
                            Case "flash"
                                'Lecteur Flash 8 via Javascript
                                patternpage.AppendLine("<!-- Intégration d'un lecteur Flash via Javascript -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<NOSCRIPT><P ALIGN=CENTER>Javascript et Flash Player 8.0 sont nécessaires pour démarrer la lecture.</P></NOSCRIPT>")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<SCRIPT LANGUAGE=""javascript"" SRC=""/swfobject.js""></SCRIPT>")
                                patternpage.AppendLine("<BR>")
                                patternpage.AppendLine("<DIV ID=""mainplayer"" ALIGN=""center"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ STYLE=""background-color: black; border-radius: 8px; width: " & player_width.ToString & "px; height: " & player_height.ToString & "px; min-width: 160px; min-height: 120px;""></DIV>")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<SCRIPT LANGUAGE=""javascript"">")
                                patternpage.AppendLine(" var so4 = new SWFObject('/player.swf','mpl','" & player_width.ToString & "','" & player_height.ToString & "','8');")
                                patternpage.AppendLine(" so4.addParam('allowscriptaccess','always');")
                                patternpage.AppendLine(" so4.addParam('allowfullscreen','true');")
                                patternpage.AppendLine(" so4.addVariable('width','" & player_width.ToString & player_prop & "');")
                                patternpage.AppendLine(" so4.addVariable('height','" & player_height.ToString & player_prop & "');")
                                patternpage.AppendLine(" so4.addVariable('file','" & GetHost() & "v/" & output_filename & "');")
                                patternpage.AppendLine(" so4.addVariable('searchbar','false');")
                                If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine(" so4.addVariable('loop','true');")
                                patternpage.AppendLine(" so4.addVariable('linkfromdisplay','true');")
                                patternpage.AppendLine()

                                patternpage.AppendLine(" so4.write('mainplayer');")
                                patternpage.AppendLine("</SCRIPT>")
                                patternpage.AppendLine("<BR>")
                                patternpage.AppendLine()
                            Case "embed_flash"
                                'Flash via <embed>
                                patternpage.AppendLine("<!-- Embarcation directe du lecteur Flash -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<EMBED SRC=""/player.swf"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ ID=""mainplayer"" allowfullscreen=""true"" allowscriptaccess=""always"" flashvars=""file=" & GetHost() & "v/" & output_filename & """ type=""application/x-shockwave-flash"" />")
                            Case "activex_flash"
                                'Flash via ActiveX
                                patternpage.AppendLine("<!-- Intégration d'un objet ActiveX pour le lecteur Flash Player -->")
                                patternpage.AppendLine()

                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" CLASSID=""clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """>")
                                patternpage.AppendLine(" <PARAM NAME=""movie"" VALUE=""/player.swf"">")
                                patternpage.AppendLine(" <PARAM NAME=""allowfullscreen"" VALUE=""true"">")
                                If vt = RequestVideoType.ShortVideo Then patternpage.AppendLine(" <PARAM NAME=""loop"" VALUE=""true"">")
                                patternpage.AppendLine(" <PARAM NAME=""allowscriptaccess"" VALUE=""always"">")
                                patternpage.AppendLine(" <PARAM NAME=""flashvars"" VALUE=""file=" & GetHost() & "v/" & output_filename & "%26searchbar=false%26linkfromdisplay=true"">")
                                patternpage.AppendLine(" <PARAM NAME=""wmode"" VALUE=""opaque"">")
                                patternpage.AppendLine("</OBJECT>")
                                patternpage.AppendLine()
                            Case "object"
                                'Objet générique sans ActiveX
                                patternpage.AppendLine("<!-- Intégration d'un média de façon générique via Object -->")
                                patternpage.AppendLine()
                                patternpage.AppendLine("<OBJECT ID=""mainplayer"" DATA=""" & GetHost() & "v/" & output_filename & """ SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """></OBJECT>")
                                patternpage.AppendLine()
                            Case Else
                                'Si par mésaventure, le paramètre manque, affichage d'un lecteur générique.
                                patternpage.AppendLine("<!-- Fallback vers une intégration générique via la balise HTML embed -->")
                                patternpage.AppendLine()
                                patternpage.AppendLine("<EMBED ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ autostart=""true"" LOOP=""" & IIf(vt = RequestVideoType.ShortVideo, "true", "false") & """ />")
                        End Select

                        If vt = RequestVideoType.WatchVideo Or vt = RequestVideoType.ShortVideo Then
                            If player_size = "fullscreen" Or player_size = "fulljs" Then
                                patternpage.AppendLine("</BODY></HTML>")
                            Else
                                'Si on ne supprime pas le fichier des commentaires de façon systématique, les commentaires s'accumulent et se répètent.
                                If IO.File.Exists(CurDir() & "\comments\" & GetMD5(last_view) & ".json") Then
                                    IO.File.Delete(CurDir() & "\comments\" & GetMD5(last_view) & ".json")
                                End If

                                Dim op_get_comments As OutputResponse = LaunchProcess("--write-comments --no-download ""https://www.youtube.com/watch?v=" & last_view & """ --no-write-info-json --extractor-args ""youtube:max_comments=500,max_comment_depth=1"" --print-to-file ""after_filter:%(comments)j"" """ & CurDir() & "\comments\" & GetMD5(last_view) & ".json"" --parse-meta ""video::(?P<comments>)""")
                                Dim output4 As String = op_get_comments.OutputData
                                Dim err4 As String = op_get_comments.ErrorData
                                Dim acc_com As String = String.Empty
                                Dim total_comments As Integer = 0

                                If IO.File.Exists(CurDir() & "\comments\" & GetMD5(last_view) & ".json") AndAlso FileLen(CurDir() & "\comments\" & GetMD5(last_view) & ".json") > 6 AndAlso disp_comments_per_video > 0 Then
                                    WriteLog("Lecture du fichier JSON contenant les commentaires de la vidéo...")
                                    Dim output_comments As String = IO.File.ReadAllText(CurDir() & "\comments\" & GetMD5(last_view) & ".json")
                                    Dim cid1, cid2 As Integer
                                    cid1 = 0
                                    cid2 = 0

                                    Do
                                        cid1 = output_comments.IndexOf("{""id"":", cid2)
                                        If cid1 = -1 Then Exit Do
                                        cid2 = output_comments.IndexOf("}", cid1)
                                        If cid1 >= cid2 Or cid1 = -1 Then Exit Do

                                        total_comments += 1

                                        If total_comments < disp_comments_per_video Then
                                            Dim one_comment As String = output_comments.Substring(cid1, cid2 - cid1)
                                            one_comment = one_comment.Replace("\""", "&quot;")
                                            Dim com_author As String = "(Auteur inconnu)"
                                            Dim com_content As String = "(Contenu indisponible)"
                                            Dim com_date As String = "(Date inconnue)"
                                            Dim com_likes As String = "0"
                                            Dim com_channel As String = "about:blank"
                                            Dim param1, param2 As Integer

                                            'Trouver l'auteur
                                            param1 = one_comment.IndexOf("""author"": ""@")
                                            If param1 >= 0 Then
                                                param2 = one_comment.IndexOf("""", param1 + 12)
                                                com_author = one_comment.Substring(param1 + 11, param2 - param1 - 11)
                                            End If

                                            param1 = one_comment.IndexOf("""text"": """)
                                            If param1 >= 0 Then
                                                param2 = one_comment.IndexOf("""", param1 + 9)
                                                com_content = one_comment.Substring(param1 + 9, param2 - param1 - 9)
                                                com_content = UnicodeJson(com_content)
                                            End If

                                            param1 = one_comment.IndexOf("""_time_text"": """)
                                            If param1 >= 0 Then
                                                param2 = one_comment.IndexOf("""", param1 + 15)
                                                com_date = one_comment.Substring(param1 + 15, param2 - param1 - 15)
                                                com_date = com_date.Replace(" ago", String.Empty)
                                                com_date = com_date.Replace("years", "ans")
                                                com_date = com_date.Replace("year", "an")
                                                com_date = com_date.Replace("days", "jours")
                                                com_date = com_date.Replace("day", "jour")
                                                com_date = com_date.Replace("months", "mois")
                                                com_date = com_date.Replace("month", "mois")
                                                com_date = com_date.Replace("hours", "heures")
                                                com_date = com_date.Replace("hour", "heure")
                                                com_date = com_date.Replace("weeks", "semaines")
                                                com_date = com_date.Replace("week", "semaine")
                                                com_date = com_date.Replace("(edited)", "(modifié)")
                                                com_date = "il y a " & com_date
                                            End If

                                            param1 = one_comment.IndexOf("""like_count"": """)
                                            If param1 > -1 Then
                                                param2 = one_comment.IndexOf(",", param1 + 14)
                                                com_likes = one_comment.Substring(param1 + 14, param2 - param1 - 14)
                                            End If

                                            param1 = one_comment.IndexOf("""author_id"": """)
                                            If param1 > -1 Then
                                                param2 = one_comment.IndexOf("""", param1 + 14)
                                                com_channel = "/channel.cgi?id=" & one_comment.Substring(param1 + 14, param2 - param1 - 14) & "&amp;section=videos"
                                            End If

                                            'acc_com &= "<HR WIDTH=100% />" & vbCrLf
                                            acc_com &= "<P><B>Par <A HREF=""" & com_channel & """ STYLE=""color: " & link_color & """>" & com_author & "</A>, " & com_date & " :</B><BR>" & vbCrLf
                                            acc_com &= com_content & vbCrLf
                                            If com_likes <> "0" Then
                                                acc_com &= "<B><IMG SRC=""th_up.gif"" ALT=""Pouce vert"" />&nbsp;<FONT COLOR=GREEN>" & com_likes & " utilisateur(s) ont aimé ce message.</FONT></B>" & vbCrLf
                                            End If
                                            acc_com &= "</P><BR>" & vbCrLf & vbCrLf
                                        End If
                                    Loop

                                    WriteLog("Il y a " & total_comments.ToString & " commentaire(s) sur cette vidéo.", ConsoleColor.Blue)
                                End If

                                If vt = RequestVideoType.WatchVideo Then
                                    patternpage.AppendLine("<BR>")
                                    patternpage.Append("<P><B>Vidéo publiée le " & tmp_prop.DateOfRelease & " par <A HREF=""" & tmp_prop.Channel_URL & """>" & tmp_prop.Creator & "</A> | " & tmp_prop.Views.Replace(" ", "&nbsp;") & " vue(s)")
                                    If Not LCase(tmp_prop.Like_Count).StartsWith("na") Or Not LCase(tmp_prop.Dislike_Count).StartsWith("na") Then patternpage.Append(" |")
                                    If Not LCase(tmp_prop.Like_Count).StartsWith("na") Then patternpage.Append(" <IMG SRC=""th_up.gif"" ALT=""Avis positifs"" />&nbsp;<FONT COLOR=""#008000"">" & GetThousands(tmp_prop.Like_Count).Replace(" ", "&nbsp;") & "</FONT>")
                                    If Not LCase(tmp_prop.Dislike_Count).StartsWith("na") Then patternpage.Append(" <IMG SRC=""th_down.gif"" ALT=""Avis négatifs"" />&nbsp;<FONT COLOR=""#800000"">" & GetThousands(tmp_prop.Dislike_Count).Replace(" ", "&nbsp;") & "</FONT>")
                                    patternpage.AppendLine("</B></P>")
                                    patternpage.AppendLine("<P STYLE=""text-align: justify;"">" & tmp_prop.Description & "</P><BR>")

                                    Dim p_unit As String = player_prop
                                    If String.IsNullOrEmpty(p_unit) Then p_unit = "px"

                                    If total_comments = 0 Then
                                        patternpage.AppendLine("<P><H2 CLASS=""black_label"" STYLE=""width: " & player_width.ToString & p_unit & ";"">Commentaires :</H2></P><BR>")
                                        If disp_comments_per_video = 0 Then
                                            patternpage.AppendLine("<P>Commentaires désactivés par l'utilisateur.</P>")
                                        Else
                                            patternpage.AppendLine("<P>Il n'y a aucun commentaire à afficher, ou ils ont été désactivés par l'auteur de la vidéo.</P>")
                                        End If
                                    Else
                                        patternpage.AppendLine("<P><H2 CLASS=""black_label"" STYLE=""width: " & player_width.ToString & p_unit & ";"">Commentaires (" & total_comments.ToString & " au total) :</H2></P><BR>")
                                    End If

                                    'Afficher tous les commentaires
                                    patternpage.AppendLine(acc_com)
                                    If total_comments > 0 Then patternpage.AppendLine("<P><A HREF=""/com.cgi?v=" & last_view & """ TARGET=""_blank"">Afficher tous les commentaires</A></P>")
                                    patternpage.AppendLine("  </TD>")
                                End If

                                'Volet droit (suggestions de vidéos à visionner, boutons des shorts, et parcours de playlists)
                                If right_panel Then
                                    If vt = RequestVideoType.ShortVideo Then
                                        Dim actual_index As Integer = -1
                                        Dim max_videos As Integer = 1
                                        patternpage.AppendLine("  <TD WIDTH=64 ALIGN=CENTER VALIGN=MIDDLE>")
                                        If LCase(tmp_prop.Like_Count) <> "na" Then patternpage.AppendLine("   <P><IMG SRC=""th_up.gif"" ALT=""Likes"" />&nbsp;<FONT COLOR=GREEN><B>" & GetThousands(tmp_prop.Like_Count) & "</B></FONT></P>")
                                        If LCase(tmp_prop.Dislike_Count) <> "na" Then patternpage.AppendLine("   <P><IMG SRC=""th_down.gif"" ALT=""Dislikes"" />&nbsp;<FONT COLOR=DARKRED><B>" & GetThousands(tmp_prop.Dislike_Count) & "</B></FONT></P>")
                                        patternpage.AppendLine("<BR>")

                                        If String.IsNullOrEmpty(current_list) Then
                                            patternpage.AppendLine("   <IMG SRC=""s_dis_up.gif"" ALT=""Arrow Up Disabled"" /><BR><BR>")
                                            patternpage.AppendLine("   <IMG SRC=""s_dis_dw.gif"" ALT=""Arrow Down Disabled"" /><BR><BR>")
                                        Else
                                            Dim op_short As OutputResponse = Nothing

                                            op_short = LaunchProcess("--flat-playlist --print ""%(id)s<|>"" ""https://www.youtube.com/channel/" & current_list & "/shorts/""", , , , 600000)

                                            Dim output5 As String = op_short.OutputData
                                            output5 = output5.Replace(vbLf, String.Empty)
                                            output5 = output5.Replace(vbCr, String.Empty)
                                            If output5.EndsWith("<|>") Then output5 = output5.Remove(output5.Length - 3, 3)
                                            Dim err5 As String = op_short.ErrorData

                                            If String.IsNullOrEmpty(output5) OrElse output5.Length = 0 OrElse output5.StartsWith("null") Then
                                                patternpage.AppendLine("   <IMG SRC=""s_dis_up.gif"" ALT=""Arrow Up Disabled"" /><BR><BR>")
                                                patternpage.AppendLine("   <IMG SRC=""s_dis_dw.gif"" ALT=""Arrow Down Disabled"" /><BR><BR>")
                                            Else
                                                Dim short_list() As String = output5.Split("<|>")
                                                max_videos = short_list.Length

                                                For i As Integer = 0 To short_list.Length - 1
                                                    If short_list(i) = tmp_prop.ID Then
                                                        actual_index = i
                                                        Exit For
                                                    End If
                                                Next

                                                If actual_index = -1 Then
                                                    patternpage.AppendLine("   <IMG SRC=""s_dis_up.gif"" ALT=""Arrow Up Disabled"" /><BR><BR>")
                                                    patternpage.AppendLine("   <IMG SRC=""s_dis_dw.gif"" ALT=""Arrow Down Disabled"" /><BR><BR>")
                                                Else
                                                    If short_list.Length = 1 Then
                                                        patternpage.AppendLine("   <IMG SRC=""s_dis_up.gif"" ALT=""Arrow Up Disabled"" /><BR><BR>")
                                                        patternpage.AppendLine("   <IMG SRC=""s_dis_dw.gif"" ALT=""Arrow Down Disabled"" /><BR><BR>")
                                                    Else
                                                        If actual_index = 0 Then
                                                            patternpage.AppendLine("   <IMG SRC=""s_dis_up.gif"" ALT=""Arrow Up Disabled"" /><BR><BR>")
                                                            patternpage.AppendLine("   <A HREF=""/short?v=" & short_list(actual_index + 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_dw.gif"" ALT=""Arrow Down"" BORDER=0 /></A><BR><BR>")
                                                        ElseIf actual_index = short_list.Length - 1 Then
                                                            patternpage.AppendLine("   <A HREF=""/short?v=" & short_list(actual_index - 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_up.gif"" ALT=""Arrow Up"" BORDER=0 /></A><BR><BR>")
                                                            patternpage.AppendLine("   <IMG SRC=""s_dis_dw.gif"" ALT=""Arrow Down Disabled"" /><BR><BR>")
                                                        Else
                                                            patternpage.AppendLine("   <A HREF=""/short?v=" & short_list(actual_index - 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_up.gif"" ALT=""Arrow Up"" BORDER=0 /></A><BR><BR>")
                                                            patternpage.AppendLine("   <A HREF=""/short?v=" & short_list(actual_index + 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_dw.gif"" ALT=""Arrow Down"" BORDER=0 /></A><BR><BR>")
                                                        End If
                                                    End If
                                                End If
                                            End If

                                        End If

                                        If total_comments > 0 Then
                                            patternpage.AppendLine("   <A HREF=""/com.cgi?v=" & last_view & "&type=short"" TARGET=""_blank""><IMG SRC=""s_ena_ms.gif"" ALT=""Comments Disabled"" BORDER=0 /></A><BR><BR>")
                                        Else
                                            patternpage.AppendLine("   <IMG SRC=""s_dis_ms.gif"" ALT=""Comments Disabled"" /><BR><BR>")
                                        End If

                                        If display_stream_button Then patternpage.AppendLine("   <A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""s_stream.gif"" ALT=""Direct Stream"" BORDER=0 /></A>")

                                        patternpage.AppendLine("<BR><BR>")
                                        Dim a_views As String = IIf(IsNumeric(tmp_prop.Views), CInt(tmp_prop.Views), "0")
                                        If max_videos > 1 Then patternpage.AppendLine("<CENTER><P><B>SHORT</B><BR>N°" & GetThousands(CStr(actual_index)).Replace(" ", "&nbsp;") & "/" & GetThousands(max_videos).Replace(" ", "&nbsp;") & "</P></CENTER>")
                                        If total_comments >= 0 Then patternpage.AppendLine("<CENTER><P><B>COMM.</B><BR>" & GetThousands(total_comments.ToString).Replace(" ", "&nbsp;") & "</P></CENTER>")
                                        patternpage.AppendLine("<CENTER><P><B>VUE(S)</B><BR>" & GetThousands(a_views).Replace(" ", "&nbsp;") & "</P></CENTER>")
                                        patternpage.AppendLine("  </TD>")
                                        patternpage.AppendLine(" </TR>")
                                        patternpage.AppendLine()
                                        patternpage.AppendLine(" <TR>")
                                        patternpage.AppendLine("  <TD>")
                                        patternpage.AppendLine("   <DIV CLASS=bodysep></DIV>")
                                        patternpage.AppendLine("  </TD>")
                                        patternpage.AppendLine(" </TR>")
                                    Else
                                        If String.IsNullOrEmpty(current_list) Then
                                            If String.IsNullOrEmpty(req) = False Then
                                                patternpage.AppendLine("  <TD WIDTH=400>")
                                                '***************************************************************************************************************************************************

                                                Dim op_getvid As OutputResponse = LaunchProcess("--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<|>%(channel_id)s<|>%(like_count)s<|>%(dislike_count)s<||>"" ""ytsearch" & number_of_results.ToString & ":" & req & """")
                                                Dim output As String = op_getvid.OutputData 'Récupération des résultats
                                                output = output.Replace(vbLf, String.Empty)
                                                output = output.Replace(vbCr, String.Empty)
                                                If output.EndsWith("<||>") Then output = output.Remove(output.Length - 4, 4)
                                                Dim err As String = op_getvid.ErrorData

                                                'Récupération des lignes

                                                If String.IsNullOrEmpty(output) Then
                                                    patternpage.AppendLine(" <P ALIGN=CENTER><B>Aucun contenu relatif à cette vidéo n'a été trouvé !</B></P>")
                                                Else
                                                    output = output.Remove(output.Length - 4, 4)
                                                    Dim lines As String() = output.Split("<||>", StringSplitOptions.RemoveEmptyEntries)

                                                    If lines.Count = 0 Then
                                                        'S'il n'y a aucune ligne retournée.
                                                        patternpage.AppendLine(" <P ALIGN=CENTER><B>Aucun contenu relatif à cette vidéo n'a été trouvé !</B></P>")
                                                    Else
                                                        'Sinon, on affiche les résultats dans la page Web.
                                                        patternpage.AppendLine("  <TABLE BORDER=0 CELLPADDING=8 CELLSPACING=0 WIDTH=400 ALIGN=CENTER>")

                                                        WriteLog("La recherche relative du mot-clef '" & req & "' a donné " & lines.Count.ToString & " résultat(s).")

                                                        For Each line In lines
                                                            line = line.Replace(vbLf, String.Empty)
                                                            line = line.Replace(vbCr, String.Empty)
                                                            If line.EndsWith("<|>") Then line = line.Substring(0, line.Length - 3)
                                                            Dim parts As String() = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                                                            For i As Integer = 0 To parts.Length - 1
                                                                For j As Integer = 0 To &H1F
                                                                    parts(i) = parts(i).Replace(Chr(j), String.Empty)
                                                                Next
                                                            Next

                                                            If parts.Length = 13 Then
                                                                Dim id As String = parts(0)
                                                                Dim title As String = parts(1)
                                                                Dim tmp_prop2 As New VideoProperties
                                                                title = CleanText(title)

                                                                tmp_prop2.Title = CleanText(parts(1))
                                                                tmp_prop2.ID = parts(0)
                                                                tmp_prop2.Views = IIf(LCase(parts(2)) = "na", "0", GetThousands(parts(2)))
                                                                tmp_prop2.DateOfRelease = GetDate(parts(3))
                                                                tmp_prop2.Creator = CleanText(parts(4))
                                                                tmp_prop2.Thumbnail = parts(5)
                                                                tmp_prop2.Like_Count = parts(11)
                                                                tmp_prop2.Dislike_Count = parts(12)

                                                                If LCase(parts(6)) = "na" Then
                                                                    tmp_prop2.Duration = -1
                                                                Else
                                                                    tmp_prop2.Duration = CInt(parts(6))
                                                                End If

                                                                tmp_prop2.Dimensions = IIf(IsNumeric(parts(7)), parts(7), "640") & ":" & IIf(IsNumeric(parts(8)), parts(8), "480")
                                                                tmp_prop2.Channel_URL = "/channel.cgi?id=" & CleanText(parts(10))

                                                                tmp_prop2.Description = IIf(String.IsNullOrEmpty(parts(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(parts(9))))
                                                                If tmp_prop2.Description.Length > 2048 Then tmp_prop2.Description = tmp_prop2.Description.Substring(0, 2048) & "..."
                                                                tmp_prop2.Description = tmp_prop2.Description.Replace(vbCrLf, "<BR>")
                                                                tmp_prop2.Description = tmp_prop2.Description.Replace(vbCr, "<BR>")
                                                                tmp_prop2.Description = tmp_prop2.Description.Replace(vbLf, "<BR>")
                                                                tmp_prop2.DateAdded = Now

                                                                SyncLock video_props
                                                                    If Not video_props.ContainsKey(id) Then
                                                                        Try
                                                                            If video_props.Count > 1000 Then
                                                                                Do Until video_props.Count = 1000
                                                                                    video_props.Remove(video_props.Keys(0))
                                                                                Loop
                                                                            End If

                                                                            video_props.Add(id, tmp_prop2)
                                                                        Catch ex As Exception

                                                                        End Try
                                                                    End If
                                                                End SyncLock

                                                                'Affichage d'une ligne dans les recherches, sous la forme d'une miniature accompagnée de quelques métadonnées.
                                                                If parts(0) <> last_view Then
                                                                    patternpage.AppendLine("   <TR CLASS=""survol"">")
                                                                    patternpage.AppendLine("    <TD WIDTH=160 HEIGHT=100>")
                                                                    patternpage.AppendLine("     <A HREF=""/watch?v=" & id & """ TARGET=""_parent""><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop2.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop2.Duration).Replace(":", "_") & """ WIDTH=160 HEIGHT=120 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A>")
                                                                    patternpage.AppendLine("    </TD>")
                                                                    patternpage.AppendLine("    <TD WIDTH=* VALIGN=TOP>")
                                                                    patternpage.Append("     <A HREF=""/watch?v=" & id & """ TARGET=""_parent"">" & EscapeHtml(title) & "</A>")
                                                                    If display_stream_button Then patternpage.Append(" <A HREF=""/stream?v=" & id & """><IMG SRC=""playbtn.gif"" ALT=""Flux direct"" BORDER=0 /></A>")
                                                                    patternpage.AppendLine()
                                                                    patternpage.AppendLine("<BR>Par <A HREF=""" & tmp_prop2.Channel_URL & """>" & tmp_prop2.Creator & "</A><BR>Date:&nbsp;" & tmp_prop2.DateOfRelease)
                                                                    patternpage.AppendLine("    </TD>")
                                                                    patternpage.AppendLine("   </TR>")
                                                                End If
                                                            End If
                                                        Next

                                                        patternpage.AppendLine("  </TABLE>")
                                                    End If
                                                End If

                                                patternpage.AppendLine("  </TD>")
                                                patternpage.AppendLine(" </TR>")
                                            End If
                                        Else
                                            patternpage.AppendLine("  <TD WIDTH=400 ALIGN=CENTER VALIGN=TOP>")
                                            patternpage.AppendLine("   <TABLE CELLPADDING=8 CELLSPACING=0 BORDER=0 VALIGN=TOP>") 'Encore une table dans une table !!!
                                            'Volet droit pour la playlist en cours de lecture

                                            'Liste des vidéos de la playlist
                                            Dim op_playlist_2 As OutputResponse = LaunchProcess("--flat-playlist --print ""%(id)s<|>"" ""https://www.youtube.com/playlist?list=" & current_list & """")
                                            Dim output5 As String = op_playlist_2.OutputData
                                            Dim found_index As Integer = -1
                                            Dim max_index As Integer = 1
                                            output5 = output5.Replace(vbLf, String.Empty)
                                            output5 = output5.Replace(vbCr, String.Empty)
                                            If output5.EndsWith("<|>") Then output5 = output5.Remove(output5.Length - 3, 3)
                                            Dim err5 As String = op_playlist_2.ErrorData

                                            If String.IsNullOrEmpty(output5) OrElse output5.Length = 0 OrElse output5.StartsWith("null") Then
                                                patternpage.AppendLine("    <TR><TD><P>La playlist demandée n'existe pas ou est indisponible. Veuillez spécifier un identifiant de playlist valide.</P></TD></TR>")
                                            Else
                                                Dim playlist_vids() As String = output5.Split("<|>")
                                                Dim final_playlist As New List(Of String) 'Compilation de 5 vidéos dans la liste
                                                max_index = playlist_vids.Length

                                                Dim pt As String = String.Empty

                                                If playlist_list.ContainsKey(current_list) Then
                                                    pt = "'" & playlist_list(current_list) & "'"
                                                End If

                                                For i As Integer = 0 To playlist_vids.Length - 1
                                                    If playlist_vids(i) = last_view Then
                                                        found_index = i
                                                        Exit For
                                                    End If
                                                Next

                                                If String.IsNullOrEmpty(pt) Then
                                                    patternpage.AppendLine("   <TR><TD WIDTH=* COLSPAN=2><P><FONT SIZE=3><CENTER><B><SPAN STYLE=""display: block; background-color: black; color: white; padding: 12px 12px 12px 12px; border-radius: 4px;"">Navigation dans la playlist actuelle&nbsp;:</SPAN><BR><BR>")
                                                Else
                                                    patternpage.AppendLine("   <TR><TD WIDTH=* COLSPAN=2><P><FONT SIZE=3><CENTER><B><SPAN STYLE=""display: block; background-color: black; color: white; padding: 12px 12px 12px 12px; border-radius: 4px;"">Navigation dans la playlist<BR>" & UnicodeJson(pt) & "</SPAN><BR><BR>")
                                                End If

                                                patternpage.Append("Lecture de la vidéo " & CStr(found_index + 1) & " sur " & playlist_vids.Count.ToString)
                                                patternpage.AppendLine("<BR><BR>")

                                                If found_index = -1 Or playlist_vids.Count = 1 Then
                                                    patternpage.Append("<IMG SRC=""s_dis_up.gif"" ALT=""Up Arrow Disabled"" />&nbsp;")
                                                    patternpage.Append("<IMG SRC=""s_dis_dw.gif"" ALT=""Down Arrow Disabled"" />")
                                                ElseIf found_index = 0 Then
                                                    patternpage.Append("<IMG SRC=""s_dis_up.gif"" ALT=""Up Arrow Disabled"" />&nbsp;")
                                                    patternpage.Append("<A HREF=""/watch?v=" & playlist_vids(found_index + 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_dw.gif"" ALT=""Down Arrow"" BORDER=0 /></A>&nbsp;")
                                                ElseIf found_index = playlist_vids.Count - 1 Then
                                                    patternpage.Append("<A HREF=""/watch?v=" & playlist_vids(found_index - 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_up.gif"" ALT=""Up Arrow"" BORDER=0 /></A>&nbsp;")
                                                    patternpage.Append("<IMG SRC=""s_dis_dw.gif"" ALT=""Down Arrow Disabled"" />")
                                                Else
                                                    patternpage.Append("<A HREF=""/watch?v=" & playlist_vids(found_index - 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_up.gif"" ALT=""Up Arrow"" BORDER=0 /></A>&nbsp;")
                                                    patternpage.Append("<A HREF=""/watch?v=" & playlist_vids(found_index + 1) & "&amp;list=" & current_list & """><IMG SRC=""s_ena_dw.gif"" ALT=""Down Arrow"" BORDER=0 /></A>")
                                                End If

                                                patternpage.AppendLine("</B></FONT></CENTER></P></TD></TR>")

                                                Dim first As Integer
                                                Dim last As Integer

                                                If playlist_vids.Length <= 5 Then
                                                    first = 0
                                                    last = playlist_vids.Length - 1
                                                Else
                                                    first = Math.Max(0, found_index - 2)
                                                    last = Math.Min(playlist_vids.Length - 1, found_index + 2)

                                                    If last - first < 4 Then
                                                        If first = 0 Then
                                                            last = 4
                                                        Else
                                                            first = playlist_vids.Length - 5
                                                        End If
                                                    End If
                                                End If

                                                For i = first To last
                                                    final_playlist.Add(playlist_vids(i))
                                                Next

                                                For Each l As String In final_playlist
                                                    Dim playlist_vid_prop As VideoProperties = GetVideo(l)
                                                    patternpage.AppendLine("   <TR CLASS=""survol"">")

                                                    If l = last_view Then
                                                        patternpage.AppendLine("    <TD WIDTH=160 HEIGHT=100 BGCOLOR=""#80C0FF"">")
                                                    Else
                                                        patternpage.AppendLine("    <TD WIDTH=160 HEIGHT=100>")
                                                    End If

                                                    If l <> last_view Then patternpage.Append("     <A HREF=""/watch?v=" & playlist_vid_prop.ID & "&amp;list=" & current_list & """>")
                                                    If l = last_view Then patternpage.Append("     ")
                                                    patternpage.Append("<IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(playlist_vid_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(playlist_vid_prop.Duration).Replace(":", "_") & """ WIDTH=160 HEIGHT=120 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(playlist_vid_prop.Title) & """ />")
                                                    If l <> last_view Then patternpage.Append("</A>")
                                                    patternpage.AppendLine()
                                                    patternpage.AppendLine("    </TD>")

                                                    If l = last_view Then
                                                        patternpage.AppendLine("    <TD WIDTH=* VALIGN=TOP BGCOLOR=""#80C0FF"">")
                                                    Else
                                                        patternpage.AppendLine("    <TD WIDTH=* VALIGN=TOP>")
                                                    End If

                                                    If l <> last_view Then patternpage.Append("     <A HREF=""/watch?v=" & playlist_vid_prop.ID & "&amp;list=" & current_list & """ TARGET=""_parent"">")
                                                    If l = last_view Then patternpage.Append("     <B>")
                                                    patternpage.Append(EscapeHtml(playlist_vid_prop.Title))
                                                    If l = last_view Then patternpage.Append("</B>")
                                                    If l <> last_view Then patternpage.AppendLine("</A>")
                                                    If display_stream_button Then patternpage.Append("     <A HREF=""/stream?v=" & playlist_vid_prop.ID & """><IMG SRC=""playbtn.gif"" ALT=""Flux direct"" BORDER=0 /></A>")
                                                    patternpage.AppendLine()
                                                    patternpage.AppendLine("    <BR>Par <A HREF=""" & playlist_vid_prop.Channel_URL.Replace("section=videos", "section=playlists") & """>" & playlist_vid_prop.Creator & "</A><BR>Date:&nbsp;" & playlist_vid_prop.DateOfRelease & "<BR>")
                                                    If l = last_view Then patternpage.AppendLine("    <FONT COLOR=DARKRED><B>(&#9658; En cours de lecture)</B></FONT>")
                                                    patternpage.AppendLine("    </TD>")
                                                    patternpage.AppendLine("   </TR>")
                                                Next
                                            End If

                                            patternpage.AppendLine("    <TR><TD COLSPAN=2><BR><CENTER><A HREF=""/playlist.cgi?id=" & current_list & """>Afficher la playlist complète</A></CENTER></TD></TR>")
                                            patternpage.AppendLine("   </TABLE>")
                                            patternpage.AppendLine("  </TD>")
                                            patternpage.AppendLine(" </TR>")
                                        End If
                                    End If
                                End If

                                patternpage.AppendLine("</TABLE></CENTER><BR><BR><BR>")
                                patternpage.AppendLine(footer)
                            End If
                        End If

                        Dim watch_resp As String =
                            "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                            "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                            "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                            "Connection: close" & vbCrLf &
                            "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                        Dim watch_bytes As Byte() = iso.GetBytes(watch_resp)

                        Try
                            stream.Write(watch_bytes, 0, watch_bytes.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        End Try
                    Else
                        'Envoi du flux direct, sans formatage HTML, ni intégration.
                        WriteLog("Streaming demandé pour la vidéo. Envoi du flux direct.", ConsoleColor.Green, client)

                        Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>La vidéo est disponible à cette URL: <A HREF=""/v/" & output_filename & """>Cliquez ici</A>.</P>" & vbCrLf

                        Dim index_resp As String = "HTTP/1.1 302 Found" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                        "Location: /v/" & output_filename & vbCrLf &
                        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

                        Dim index_data As Byte() = iso.GetBytes(index_resp)

                        Try
                            stream.Write(index_data, 0, index_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                        End Try

                        client.Close()
                        Exit Sub
                    End If
                Else
                    Dim notfound_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet, et ne peut donc pas traiter cette requête.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                    Try
                        stream.Write(notfound_data, 0, notfound_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try
                End If
            Else
                'Identifiant invalide manifestement!
                patternpage.AppendLine(InitValues("Erreur de saisie", , wanted_skin, , used_player))
                patternpage.AppendLine(" <P ALIGN=CENTER><BR><B>L'identifiant vidéo que vous avez entré semble invalide. Aucune lecture ne peut être poursuivie.<BR><BR>Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</B></P><BR><BR></BODY></HTML>")
                patternpage.AppendLine()

                Dim watch_resp As String =
                    "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                Dim baddata As Byte() = iso.GetBytes(watch_resp)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try
            End If

            client.Close()
        ElseIf request.StartsWith("GET /watch") Then
            'Requête vide
            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>Veuillez vous rendre sur ce <A HREF=""/feed"">lien</A> pour effectuer une recherche.</P>" & vbCrLf

            Dim index_resp As String =
                "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Location: /" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /stream") Then
            'Requête vide
            Dim last_host_2 As String = GetHost()

            If last_host_2 = "/" Then last_host_2 = "127.0.0.1"

            Dim result_page As String = "<H1>Erreur 400 - Requête erronée</H1><P>Vous devez préciser quel vidéo lire directement en flux, avec le paramètre <I>v</I>.<BR>Ex: http://" & last_host_2 & "/stream?v=BbCefdlDDTU<BR><BR>" & vbCrLf & "Click <A HREF=""/feed"">here</A> to go back to the index page.</P>" & vbCrLf

            Dim index_resp As String =
                "HTTP/" & http_ver & " 400 Bad Request" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /search?q=") Or request.StartsWith("GET /lucky?q=") Then
            'Lancement d'une recherche par l'utilisateur.

            Dim get_params As String = String.Empty

            If IsNetworkAvailable() Then
                Dim arg As String = Split(request)(1)

                If request.StartsWith("GET /search") Then
                    arg = arg.Remove(0, 7)
                    vt = RequestVideoType.SearchVideo
                Else
                    arg = arg.Remove(0, 6)
                    vt = RequestVideoType.LuckyVideo
                End If

                'Les caractères systèmes sont retirés par sécurité
                For i As Integer = 0 To &H1F
                    request = request.Replace(Chr(i), String.Empty)
                Next

                'Récupérer les 10 vidéos en rapport avec le mot-clef spécifié
                Dim req As String = arg.Remove(0, 3)
                req = UrlDecodeLatin1(req)
                req = req.Replace("+", " ")

                If Not String.IsNullOrEmpty(req) Then
                    If req.Contains("&") And Not req.EndsWith("&") Then
                        Dim req_f As Integer = req.IndexOf("&")
                        get_params = req.Remove(0, req_f)
                        req = req.Substring(0, req_f)
                    End If

                    WriteLog("Le mot-clef '" & req & "' a été demandé. Recherche en cours...", ConsoleColor.White, client)
                    If vt = RequestVideoType.LuckyVideo Then WriteLog("Mode chanceux activé par l'utilisateur. Seul le premier résultat sera renvoyé.")

                    'Lancement de yt-dlp
                    Dim n As String = "0"

                    If vt = RequestVideoType.LuckyVideo Then
                        n = "1"
                    Else
                        n = number_of_results.ToString
                    End If

                    Dim op_getvid As OutputResponse = LaunchProcess("--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<|>%(channel_id)s<|>%(like_count)s<|>%(dislike_count)s<||>"" ""ytsearch" & n & ":" & req & """")
                    Dim output As String = op_getvid.OutputData 'Récupération des résultats
                    output = output.Replace(vbLf, String.Empty)
                    output = output.Replace(vbCr, String.Empty)
                    If output.EndsWith("<||>") Then output = output.Remove(output.Length - 4, 4)

                    Dim err As String = op_getvid.ErrorData

                    If vt <> RequestVideoType.LuckyVideo Then patternpage.Append(InitValues("Recherche de " & EscapeHtml(req), req, wanted_skin, , used_player))

                    'Récupération des lignes
                    If String.IsNullOrEmpty(output) Then
                        patternpage.AppendLine(" <P ALIGN=CENTER><BR><B><FONT SIZE=4>Aucun résultat trouvé !</FONT></B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>")
                        patternpage.AppendLine()
                        WriteLog("La recherche du mot-clef '" & req & "' n'a donné aucun résultat.")
                    Else
                        output = output.Remove(output.Length - 4, 4)
                        Dim lines As String() = output.Split("<||>", StringSplitOptions.RemoveEmptyEntries)

                        If lines.Count = 0 Then
                            'S'il n'y a aucune ligne retournée.
                            If vt = RequestVideoType.LuckyVideo Then
                                Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Ceci est un message d'erreur générique pour annoncer qu'aucune vidéo avec le(s) mot-clef(s) spécifié(s) n'a été trouvée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                                Try
                                    stream.Write(notfound_data, 0, notfound_data.Length)
                                Catch ex As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                    client.Close()
                                    Exit Sub
                                End Try

                                WriteLog("La recherche du mot-clef '" & req & "' en mode chanceux n'a donné aucun résultat.")
                                client.Close()
                                Exit Sub
                            Else
                                patternpage.AppendLine(" <P ALIGN=CENTER><BR><B><FONT SIZE=4>Aucun résultat trouvé !</FONT></B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>")
                                patternpage.AppendLine()
                                WriteLog("La recherche du mot-clef '" & req & "' n'a donné aucun résultat.")
                            End If
                        Else
                            'Sinon, on affiche les résultats dans la page Web.
                            If vt = RequestVideoType.LuckyVideo Then
                                Dim parts As String() = lines(0).Split(New String() {"<|>"}, StringSplitOptions.None)
                                '302
                                Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>Une vidéo a été trouvée en mode chanceux. Elle est disponible à cette URL: <A HREF=""/stream?v=" & parts(0) & """>Cliquez ici</A>.</P>" & vbCrLf

                                Dim index_resp As String = "HTTP/1.1 302 Found" & vbCrLf &
                                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                                "Connection: close" & vbCrLf &
                                "Location: /stream?v=" & parts(0) & get_params & vbCrLf &
                                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser.

                                Dim index_data As Byte() = iso.GetBytes(index_resp)

                                Try
                                    stream.Write(index_data, 0, index_data.Length)
                                Catch ex As Exception
                                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                                End Try

                                client.Close()
                                Exit Sub
                            Else
                                If lines.Count = 1 Then
                                    patternpage.AppendLine(" <BR><CENTER><P ALIGN=CENTER CLASS=""black_label""><B><FONT SIZE=4>Le meilleur résultat pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;»&nbsp;:</FONT></B></P></CENTER><BR><BR>")
                                    patternpage.AppendLine()
                                Else
                                    patternpage.AppendLine(" <BR><CENTER><P ALIGN=CENTER CLASS=""black_label""><B><FONT SIZE=4>Les " & lines.Count.ToString & " meilleurs résultats pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;»&nbsp;:</FONT></B></P></CENTER><BR><BR>")
                                    patternpage.AppendLine()
                                End If

                                patternpage.AppendLine("  <CENTER><TABLE BORDER=0 CELLPADDING=8 CELLSPACING=0 WIDTH=600 ALIGN=CENTER>")

                                WriteLog("La recherche du mot-clef '" & req & "' a donné " & lines.Count.ToString & " résultat(s).")

                                For Each line In lines

                                    line = line.Replace(vbLf, String.Empty)
                                    line = line.Replace(vbCr, String.Empty)
                                    If line.EndsWith("<|>") Then line = line.Substring(0, line.Length - 3)
                                    Dim parts As String() = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                                    For i As Integer = 0 To parts.Length - 1
                                        For j As Integer = 0 To &H1F
                                            parts(i) = parts(i).Replace(Chr(j), String.Empty)
                                        Next
                                    Next

                                    If parts.Length = 13 Then
                                        Dim id As String = parts(0)
                                        Dim title As String = parts(1)
                                        Dim tmp_prop As New VideoProperties
                                        title = CleanText(title)

                                        tmp_prop.Title = CleanText(parts(1))
                                        tmp_prop.ID = parts(0)
                                        tmp_prop.Views = IIf(LCase(parts(2)) = "na", "0", GetThousands(parts(2)))
                                        tmp_prop.DateOfRelease = GetDate(parts(3))
                                        tmp_prop.Creator = CleanText(parts(4))
                                        tmp_prop.Thumbnail = parts(5)
                                        tmp_prop.Channel_URL = "/channel.cgi?id=" & CleanText(parts(10)) & "&amp;section=videos"
                                        tmp_prop.Like_Count = parts(11)
                                        tmp_prop.Dislike_Count = parts(12)

                                        If LCase(parts(6)) = "na" Then
                                            tmp_prop.Duration = -1
                                        Else
                                            tmp_prop.Duration = CInt(parts(6))
                                        End If

                                        tmp_prop.Dimensions = IIf(IsNumeric(parts(7)), parts(7), "640") & ":" & IIf(IsNumeric(parts(8)), parts(8), "480")

                                        tmp_prop.Description = IIf(String.IsNullOrEmpty(parts(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(parts(9))))
                                        If tmp_prop.Description.Length > 2048 Then tmp_prop.Description = tmp_prop.Description.Substring(0, 2048) & "..."
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbCrLf, "<BR>")
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbCr, "<BR>")
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbLf, "<BR>")
                                        tmp_prop.DateAdded = Now

                                        SyncLock video_props
                                            If Not video_props.ContainsKey(id) Then
                                                Try
                                                    If video_props.Count > 1000 Then
                                                        Do Until video_props.Count = 1000
                                                            video_props.Remove(video_props.Keys(0))
                                                        Loop
                                                    End If

                                                    video_props.Add(id, tmp_prop)
                                                Catch ex As Exception

                                                End Try
                                            End If
                                        End SyncLock

                                        'Affichage d'une ligne dans les recherches, sous la forme d'une miniature accompagnée de quelques métadonnées.
                                        patternpage.AppendLine("   <TR CLASS=""survol"">")
                                        patternpage.AppendLine("    <TD WIDTH=160 HEIGHT=100>")
                                        patternpage.AppendLine("     <A HREF=""/watch?v=" & id & "&amp;lastsearch=" & Uri.EscapeDataString(req.Replace(" ", "+")) & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A>")
                                        patternpage.AppendLine("    </TD>")
                                        patternpage.AppendLine("    <TD WIDTH=* VALIGN=TOP>")
                                        patternpage.Append("     <A HREF=""/watch?v=" & id & "&amp;lastsearch=" & Uri.EscapeDataString(req.Replace(" ", "+")) & """>" & EscapeHtml(title) & "</A>")
                                        If display_stream_button Then patternpage.AppendLine(" <A HREF=""/stream?v=" & id & """><IMG SRC=""playbtn.gif"" BORDER=0 ALT=""Flux direct"" /></A>")
                                        patternpage.AppendLine()

                                        patternpage.AppendLine("<BR>")
                                        patternpage.AppendLine("     Vidéo publiée le " & tmp_prop.DateOfRelease & " par <A HREF=""" & tmp_prop.Channel_URL & """>" & tmp_prop.Creator & "</A><BR>")
                                        patternpage.AppendLine("     " & tmp_prop.Views & " vue(s)<BR></TD>")
                                        patternpage.AppendLine("   </TR>")
                                    End If
                                Next

                                patternpage.AppendLine("  </TABLE></CENTER>")
                            End If
                        End If
                    End If

                    patternpage.AppendLine("<BR><BR>" & footer)

                    'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                    Dim req_resp As String =
                        "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                        "Connection: close" & vbCrLf &
                        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                    'Conversion en octets, suivant le format ISO-8859-1.
                    Dim req_data As Byte() = iso.GetBytes(req_resp)

                    Try
                        'Ecriture dans le flux octal en direction du client.
                        stream.Write(req_data, 0, req_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try
                Else
                    'Si le mot-clef est vide voire invalide.
                    If vt = RequestVideoType.LuckyVideo Then
                        Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Message générique pour annoncer à l'utilisateur qu'aucun mot-clef n'a été spécifié.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                        Try
                            stream.Write(notfound_data, 0, notfound_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        End Try
                        client.Close()
                    Else
                        patternpage.AppendLine(InitValues("Erreur de recherche", , wanted_skin, , used_player))
                        'patternpage &= " <HR WIDTH=880 ALIGN=CENTER /><BR>" & vbCrLf
                        patternpage.AppendLine(" <CENTER><H1 CLASS=""black_label"">Message d'erreur</H1></CENTER>")
                        patternpage.AppendLine(" <P ALIGN=CENTER><BR><B><FONT SIZE=2>Veuillez spécifier un mot-clef pour que la recherche puisse avoir lieu.<BR><BR>Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</FONT></B></P><BR><BR><DIV CLASS=""bodysep""></DIV>")
                        patternpage.AppendLine()
                        patternpage.AppendLine(footer)

                        Dim req_resp As String =
                            "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                            "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                            "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                            "Connection: close" & vbCrLf &
                            "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                        Dim req_data As Byte() = iso.GetBytes(req_resp)

                        Try
                            stream.Write(req_data, 0, req_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        End Try
                    End If
                End If

                client.Close()
            Else
                Dim notfound_data As Byte() = GetHTTPBytes(500, "<H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet. Ainsi, la requête ne peut pas être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /search") Then
            'Requête vide
            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>Veuillez vous rendre <A HREF=""/feed"">ici</A> pour chercher une vidéo.</P>" & vbCrLf

            Dim index_resp As String = "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Location: /" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /lucky") Then
            'Requête vide, lucky sans paramètre renvoie un formulaire de recherche
            WriteLog("L'utilisateur demande le formulaire de recherche en mode chanceux.", , client)
            patternpage.AppendLine(InitValues("Accueil", , wanted_skin, True, used_player))
            patternpage.AppendLine("<P ALIGN=CENTER><BR><B>Faire une recherche en mode chanceux renvoie une unique vidéo basée sur des mot-clefs à rechercher dans la zone ci-dessus.<BR><BR>Cliquez <A HREF=""/about.htm"">ICI</A> pour obtenir plus d'informations sur le fonctionnement.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & footer)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /getpic.cgi?url=") Then
            'Miniatures YouTube
            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 12)

            Dim pic_params() As String = Nothing
            Dim pic_type As String = "thumbnail"
            Dim pic_url As String = "about:blank"
            Dim pic_duration As String = "?:??"
            Dim default_resource As String = "nopic.jpg"
            Dim no_duration As Boolean = True

            If arg.Contains("&") Then
                pic_params = arg.Split("&") 'Collection de paramètres
            Else
                pic_params = New String() {arg} 'Singleton
            End If

            'Extraction des paramètres de l'URL
            For Each pa As String In pic_params
                If pa.Contains("=") Then
                    Dim sub_params() As String = Split(pa, "=")
                    Select Case sub_params(0)
                        Case "url"
                            pic_url = Uri.UnescapeDataString(sub_params(1))
                            Dim pic_uri As New Uri(pic_url)
                            pic_url = pic_uri.AbsoluteUri
                        Case "duration"
                            Try
                                pic_duration = sub_params(1)
                            Catch ex As Exception
                                pic_duration = "?_??"
                            End Try
                            no_duration = False
                        Case "type"
                            If {"thumbnail", "short", "playlist", "avatar", "banner"}.Contains(sub_params(1)) Then
                                pic_type = sub_params(1)
                            Else
                                pic_type = "thumbnail"
                            End If
                    End Select
                End If
            Next

            'Si l'utilisateur met des durées pourries
            If Not IsNumeric(pic_duration.Replace("_", String.Empty)) Or pic_duration.Length > 10 Then
                pic_duration = "?_??"
            End If

            If Not pic_duration.Contains("_") Then
                pic_duration = "?_??"
            End If

            If Split(pic_duration, "_").Length > 3 Then
                pic_duration = "?_??"
            End If

            If no_duration Then pic_duration = String.Empty

            WriteLog("Ressource picturale demandée: " & IIf(String.IsNullOrEmpty(pic_url), "<null>", pic_url), , client)

            Select Case pic_type
                Case "thumbnail"
                    default_resource = "nopic.jpg"
                Case "short"
                    default_resource = "nopic.jpg" '"noshort.jpg"
                Case "avatar"
                    default_resource = "noavat.jpg"
                Case "banner"
                    default_resource = "blank.gif"
                Case Else
                    default_resource = "nopic.jpg"
            End Select

            Dim path As String = CurDir() & "\resfiles\" & default_resource

            If String.IsNullOrEmpty(pic_url) OrElse pic_url.Length = 0 OrElse pic_url = "about:blank" Then
                path = CurDir() & "\resfiles\" & default_resource
            Else
                path = CurDir() & "\thumbs\" & GetMD5(pic_url) & ".jpg"

                If pic_url.StartsWith("http://") Then
                    pic_url = "https://" & pic_url.Remove(0, 7)
                End If

                'Exclure les URL extérieures à YouTube.
                If Not pic_url.StartsWith("https://i.ytimg.com/") And Not pic_url.StartsWith("https://yt3.ggpht.com/") And Not pic_url.StartsWith("https://yt3.googleusercontent.com/") Then
                    path = CurDir() & "\resfiles\" & default_resource
                End If
            End If

            If Not IO.File.Exists(path) Then
                Try
                    Dim wc As New Net.WebClient()
                    wc.DownloadFile(pic_url, CurDir() & "\tmp_pic\" & GetMD5(pic_url) & ".jpg")

                    WriteLog("La miniature suivante a été mise en cache: " & pic_url, ConsoleColor.Green)
                Catch ex As Exception
                    path = CurDir() & "\resfiles\" & default_resource
                    WriteLog("Erreur: Pas de miniature trouvée! Envoi d'une miniature par défaut...", ConsoleColor.Red)
                End Try
            End If

            '**************************************************************************************************************************************************************************
            '***************************************************************************************** CONVERSION *********************************************************************
            '**************************************************************************************************************************************************************************

            Dim wanted_arg As String = String.Empty
            wanted_arg = "-i " & GetMD5(pic_url) & ".jpg -o " & GetMD5(pic_url) & ".jpg "

            Select Case pic_type
                Case "thumbnail"
                    wanted_arg &= "-w 160 -h 100"
                    If Not String.IsNullOrEmpty(pic_duration) AndAlso pic_duration <> "?:??" Then wanted_arg &= " -duration " & pic_duration.Replace("_", ":")
                Case "avatar"
                    wanted_arg &= "-w 64 -h 64"
                Case "banner"
                    wanted_arg &= "-w 600 -h 100"
                Case "shorts"
                    wanted_arg &= "-w 120 -h 214"
                Case Else
                    wanted_arg &= "-w 160 -h 100"
            End Select

            Dim op_image As OutputResponse = LaunchProcess(wanted_arg, "ImageTool.exe", , , 60000) '1 minute maximum

            If IO.File.Exists(CurDir() & "\thumbs\" & GetMD5(pic_url) & ".jpg") Then
                path = CurDir() & "\thumbs\" & GetMD5(pic_url) & ".jpg"
            Else
                path = CurDir() & "\resfiles\" & default_resource
            End If

            Dim bytes As Byte() = IO.File.ReadAllBytes(path)
            Dim header As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf

            If path = "\resfiles\blank.gif" Then
                header &= "Content-Type: image/gif" & vbCrLf
            Else
                header &= "Content-Type: image/jpeg" & vbCrLf
            End If

            header &= "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf &
                "Content-Length: " & bytes.Length & vbCrLf & vbCrLf

            Try
                stream.Write(iso.GetBytes(header), 0, iso.GetBytes(header).Length)
                stream.Write(bytes, 0, bytes.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /config.cgi") Or request.StartsWith("POST /config.cgi") Then 'Implémenter POST pour obtenir la page de configuration permet d'éviter de mystérieuses erreurs 400 sous IE3.
            'Montrer le panneau de configuration client du navigateur
            'message=gotreset, message=gotsaved

            WriteLog("Panneau de configuration demandé par le client.", , client)

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

            Dim selected_one As String = String.Empty
            Dim selected_five As String = String.Empty
            Dim selected_ten As String = String.Empty
            Dim selected_twenty As String = String.Empty
            Dim selected_forty As String = String.Empty

            Dim selected_channel_nine As String = String.Empty
            Dim selected_channel_eighteen As String = String.Empty
            Dim selected_channel_twentyseven As String = String.Empty

            Dim selected_enable_trends As String = String.Empty
            Dim selected_disable_trends As String = String.Empty
            Dim selected_enable_stream As String = String.Empty
            Dim selected_disable_stream As String = String.Empty

            Dim selected_disp_zero As String = String.Empty
            Dim selected_disp_ten As String = String.Empty
            Dim selected_disp_twenty As String = String.Empty
            Dim selected_disp_fifty As String = String.Empty
            Dim selected_disp_hundred As String = String.Empty

            Dim selected_micro As String = String.Empty
            Dim selected_ultrasmall As String = String.Empty
            Dim selected_small As String = String.Empty
            Dim selected_middle As String = String.Empty
            Dim selected_large As String = String.Empty
            Dim selected_cinema As String = String.Empty
            Dim selected_auto As String = String.Empty
            Dim selected_auto_height As String = String.Empty
            Dim selected_fullscreen As String = String.Empty
            Dim selected_fulljs As String = String.Empty
            Dim selected_gold1 As String = String.Empty
            Dim selected_gold2 As String = String.Empty
            Dim selected_classic_size As String = String.Empty
            Dim selected_big_cinema As String = String.Empty
            Dim selected_vertical1 As String = String.Empty
            Dim selected_vertical2 As String = String.Empty
            Dim selected_vertical3 As String = String.Empty
            Dim selected_eh As String = String.Empty 'Eight hundred
            Dim selected_ot As String = String.Empty 'One thousand
            Dim selected_otsh As String = String.Empty 'One thousand six hundred six seven six seven six seven

            Dim selected_v1 As String = String.Empty 'Lecteur pour les shorts
            Dim selected_v2 As String = String.Empty
            Dim selected_v3 As String = String.Empty
            Dim selected_v0 As String = String.Empty

            Dim selected_avi_mpeg4 As String = String.Empty
            Dim selected_avi_msvideo1 As String = String.Empty
            Dim selected_mp4 As String = String.Empty
            Dim selected_rm As String = String.Empty
            Dim selected_wmv As String = String.Empty
            Dim selected_mov_cinepak As String = String.Empty 'Cinepak MOV
            Dim selected_mov_svq1 As String = String.Empty
            Dim selected_mpg As String = String.Empty
            Dim selected_3gp As String = String.Empty
            Dim selected_flv As String = String.Empty
            Dim selected_avi_yuv As String = String.Empty
            Dim selected_oldwmv As String = String.Empty
            Dim selected_mov_mpeg4 As String = String.Empty
            Dim selected_avi_cinepak As String = String.Empty 'Cinepak AVI
            Dim selected_mov_rpza As String = String.Empty
            Dim selected_avi_mjpeg As String = String.Empty
            Dim selected_mov_mjpeg As String = String.Empty
            Dim selected_xvid As String = String.Empty
            Dim selected_mp4_legacy As String = String.Empty
            Dim selected_mpg_recent As String = String.Empty

            Dim selected_nointegration As String = String.Empty
            Dim selected_legacy_wmp As String = String.Empty
            Dim selected_wmp As String = String.Empty
            Dim selected_embed As String = String.Empty
            Dim selected_video As String = String.Empty
            Dim selected_realplayer As String = String.Empty
            Dim selected_realplayer_activex As String = String.Empty
            Dim selected_vlc As String = String.Empty
            Dim selected_vlcembed As String = String.Empty
            Dim selected_quicktime As String = String.Empty
            Dim selected_embed_quick As String = String.Empty
            Dim selected_flashplayer As String = String.Empty
            Dim selected_embedflash As String = String.Empty
            Dim selected_objectflash As String = String.Empty
            Dim selected_genobject As String = String.Empty
            Dim selected_altvlc As String = String.Empty
            Dim selected_alt_video As String = String.Empty

            Dim selected_framerateauto As String = String.Empty
            Dim selected_framerate10 As String = String.Empty
            Dim selected_framerate12 As String = String.Empty
            Dim selected_framerate15 As String = String.Empty
            Dim selected_framerate20 As String = String.Empty
            Dim selected_framerate24 As String = String.Empty
            Dim selected_framerate25 As String = String.Empty
            Dim selected_framerate30 As String = String.Empty
            Dim selected_framerate60 As String = String.Empty

            Dim selected_96p As String = String.Empty
            Dim selected_120p As String = String.Empty
            Dim selected_144p As String = String.Empty
            Dim selected_240p As String = String.Empty
            Dim selected_360p As String = String.Empty
            Dim selected_480p As String = String.Empty
            Dim selected_720p As String = String.Empty
            Dim selected_1080p As String = String.Empty
            Dim selected_autosize As String = String.Empty

            Dim selected_classic As String = String.Empty
            Dim selected_cosmic As String = String.Empty
            Dim selected_modern As String = String.Empty
            Dim selected_dark As String = String.Empty
            Dim selected_rose As String = String.Empty
            Dim selected_aqua As String = String.Empty
            Dim selected_monochrome As String = String.Empty
            Dim selected_mint As String = String.Empty
            Dim selected_sunshine As String = String.Empty

            Dim selected_nopanel As String = String.Empty
            Dim selected_panel As String = String.Empty

            Dim selected_no_long_vids As String = String.Empty
            Dim selected_long_vids As String = String.Empty

            'Nombre de résultats par recherche et affichage en paramètres
            Select Case number_of_results
                Case 1 : selected_one = " SELECTED"
                Case 5 : selected_five = " SELECTED"
                Case 10 : selected_ten = " SELECTED"
                Case 20 : selected_twenty = " SELECTED"
                Case Else : selected_ten = " SELECTED"
            End Select

            Select Case disp_comments_per_video
                Case 0 : selected_disp_zero = " SELECTED"
                Case 10 : selected_disp_ten = " SELECTED"
                Case 20 : selected_disp_twenty = " SELECTED"
                Case 50 : selected_disp_fifty = " SELECTED"
                Case 100 : selected_disp_hundred = " SELECTED"
            End Select

            'Taille de l'élément du lecteur
            Select Case player_size
                Case "micro" : selected_micro = " SELECTED"
                Case "ultrasmall" : selected_ultrasmall = " SELECTED"
                Case "small" : selected_small = " SELECTED"
                Case "middle" : selected_middle = " SELECTED"
                Case "large" : selected_large = " SELECTED"
                Case "cinema" : selected_cinema = " SELECTED"
                Case "auto" : selected_auto = " SELECTED"
                Case "autoheight" : selected_auto_height = " SELECTED"
                Case "fullscreen" : selected_fullscreen = " SELECTED"
                Case "fulljs" : selected_fulljs = " SELECTED"
                Case "gold1" : selected_gold1 = " SELECTED"
                Case "gold2" : selected_gold2 = " SELECTED"
                Case "cs" : selected_classic_size = " SELECTED"
                Case "bigcinema" : selected_big_cinema = " SELECTED"
                Case "vertical1" : selected_vertical1 = " SELECTED"
                Case "vertical2" : selected_vertical2 = " SELECTED"
                Case "vertical3" : selected_vertical3 = " SELECTED"
                Case "eh" : selected_eh = " SELECTED"
                Case "ot" : selected_ot = " SELECTED"
                Case "otsh" : selected_otsh = " SELECTED"
                Case Else : selected_middle = " SELECTED"
            End Select

            'Codec vidéo/audio utilisé pour la lecture
            Select Case used_codec
                Case "mp4" : selected_mp4 = " SELECTED"
                Case "legacy_mp4" : selected_mp4_legacy = " SELECTED"
                Case "avi_msvideo1" : selected_avi_msvideo1 = " SELECTED"
                Case "avi_yuv" : selected_avi_yuv = " SELECTED"
                Case "avi_mpeg4" : selected_avi_mpeg4 = " SELECTED"
                Case "avi_cinepak" : selected_avi_cinepak = " SELECTED"
                Case "mov_cinepak" : selected_mov_cinepak = " SELECTED"
                Case "mov_svq1" : selected_mov_svq1 = " SELECTED"
                Case "mov_mpeg4" : selected_mov_mpeg4 = " SELECTED"
                Case "mov_rpza" : selected_mov_rpza = " SELECTED"
                Case "wmv2" : selected_wmv = " SELECTED"
                Case "wmv1" : selected_oldwmv = " SELECTED"
                Case "mpeg1" : selected_mpg = " SELECTED"
                Case "3gp" : selected_3gp = " SELECTED"
                Case "flv" : selected_flv = " SELECTED"
                Case "rm" : selected_rm = " SELECTED"
                Case "mov_mjpeg" : selected_mov_mjpeg = " SELECTED"
                Case "avi_mjpeg" : selected_avi_mjpeg = " SELECTED"
                Case "recent_mpeg1" : selected_mpg_recent = " SELECTED"
                Case "xvid" : selected_xvid = " SELECTED"
                Case Else : selected_avi_mpeg4 = " SELECTED" 'Failsafe
            End Select

            'Type d'intégration utilisée pour le navigateur client
            Select Case used_player
                Case "no_integration" : selected_nointegration = " SELECTED"
                Case "legacy_wmp" : selected_legacy_wmp = " SELECTED"
                Case "wmp" : selected_wmp = " SELECTED"
                Case "embed" : selected_embed = " SELECTED"
                Case "video" : selected_video = " SELECTED"
                Case "realplayer" : selected_realplayer = " SELECTED"
                Case "activex_realplayer" : selected_realplayer_activex = " SELECTED"
                Case "embed_vlc" : selected_vlcembed = " SELECTED"
                Case "vlc" : selected_vlc = " SELECTED"
                Case "alt_vlc" : selected_altvlc = " SELECTED"
                Case "quicktime" : selected_quicktime = " SELECTED"
                Case "embed_quicktime" : selected_embed_quick = " SELECTED"
                Case "flash" : selected_flashplayer = " SELECTED"
                Case "embed_flash" : selected_embedflash = " SELECTED"
                Case "activex_flash" : selected_objectflash = " SELECTED"
                Case "object" : selected_genobject = " SELECTED"
                Case "alt_video" : selected_alt_video = " SELECTED"
                Case Else : selected_embed = " SELECTED" 'Failsafe
            End Select

            'Nombre d'images par seconde pour la vidéo lue
            Select Case frame_rate
                Case "auto" : selected_framerateauto = " SELECTED"
                Case "10" : selected_framerate10 = " SELECTED"
                Case "12" : selected_framerate12 = " SELECTED"
                Case "15" : selected_framerate15 = " SELECTED"
                Case "20" : selected_framerate20 = " SELECTED"
                Case "24" : selected_framerate24 = " SELECTED"
                Case "25" : selected_framerate25 = " SELECTED"
                Case "30" : selected_framerate30 = " SELECTED"
                Case "60" : selected_framerate60 = " SELECTED"
                Case Else : selected_framerate24 = " SELECTED" 'Failsafe
            End Select

            'Résolution utilisée pour la vidéo
            Select Case used_resolution
                Case "auto" : selected_autosize = " SELECTED"
                Case "96p" : selected_96p = " SELECTED"
                Case "120p" : selected_120p = " SELECTED"
                Case "144p" : selected_144p = " SELECTED"
                Case "240p" : selected_240p = " SELECTED"
                Case "360p" : selected_360p = " SELECTED"
                Case "480p" : selected_480p = " SELECTED"
                Case "720p" : selected_720p = " SELECTED"
                Case "1080p" : selected_1080p = " SELECTED"
                Case Else : selected_240p = " SELECTED" 'Failsafe
            End Select

            Select Case wanted_skin
                Case "oldyt" : selected_classic = " SELECTED"
                Case "cosmic" : selected_cosmic = " SELECTED"
                Case "modern" : selected_modern = " SELECTED"
                Case "dark" : selected_dark = " SELECTED"
                Case "rose" : selected_rose = " SELECTED"
                Case "aqua" : selected_aqua = " SELECTED"
                Case "mint" : selected_mint = " SELECTED"
                Case "sunshine" : selected_sunshine = " SELECTED"
                Case "monochrome" : selected_monochrome = " SELECTED"
                Case Else : selected_cosmic = " SELECTED"
            End Select

            Select Case disp_vids_per_channel
                Case 9 : selected_channel_nine = " SELECTED"
                Case 18 : selected_channel_eighteen = " SELECTED"
                Case 27 : selected_channel_twentyseven = " SELECTED"
                Case Else : selected_channel_nine = " SELECTED"
            End Select

            Select Case player_vsize
                Case "vert0" : selected_v0 = " SELECTED"
                Case "vert1" : selected_v1 = " SELECTED"
                Case "vert2" : selected_v2 = " SELECTED"
                Case "vert3" : selected_v3 = " SELECTED"
            End Select

            If right_panel Then
                selected_panel = " SELECTED"
            Else
                selected_nopanel = " SELECTED"
            End If

            If display_trends Then
                selected_enable_trends = " SELECTED"
            Else
                selected_disable_trends = " SELECTED"
            End If

            If display_stream_button Then
                selected_enable_stream = " SELECTED"
            Else
                selected_disable_stream = " SELECTED"
            End If

            If forbid_long_vids Then
                selected_no_long_vids = " SELECTED"
            Else
                selected_long_vids = " SELECTED"
            End If

            patternpage.AppendLine(InitValues("Configuration client", , wanted_skin, , used_player, , True))
            patternpage.AppendLine("<BR><CENTER><H1 CLASS=""black_label"" STYLE=""width: 780px;""><B>Configuration du client RetroYT :</B></H1></CENTER><BR>")
            patternpage.AppendLine()

            If request.Contains("message=gotreset") Then
                patternpage.AppendLine("<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été remise par défaut avec succès (" & Now.ToString & ").</FONT></B></P></CENTER><BR>")
            ElseIf request.Contains("message=gotsaved") Then
                patternpage.AppendLine("<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été enregistrée avec succès (" & Now.ToString & ").</FONT></B></P></CENTER><BR>")
            End If

            patternpage.AppendLine("  <FORM METHOD=""POST"" ACTION=""/savecfg.cgi"">")
            patternpage.AppendLine("   <CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=780>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=CENTER COLSPAN=2><CENTER><H2>Système de recherche :</H2></CENTER></TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT HEIGHT=40 WIDTH=380>Nombre de résultats affichés par recherche&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD HEIGHT=40 WIDTH=*>")
            patternpage.AppendLine("	  <SELECT NAME=""results"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""1""" & selected_one & ">1 résultat</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""5""" & selected_five & ">5 résultats</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""10""" & selected_ten & ">10 résultats [Par défaut]</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""20""" & selected_twenty & ">20 résultats</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT HEIGHT=40 WIDTH=380>Tendances YouTube dans l'index&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD HEIGHT=40 WIDTH=*>")
            patternpage.AppendLine("	  <SELECT NAME=""trends"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""enable""" & selected_enable_trends & ">Activer [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""disable""" & selected_disable_trends & ">Désactiver</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT HEIGHT=40 WIDTH=380>Afficher le lien de flux direct <IMG SRC=""playbtn.gif"" ALT=""Flux direct"" />&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD HEIGHT=40 WIDTH=*>")
            patternpage.AppendLine("	  <SELECT NAME=""displaystream"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""enable""" & selected_enable_stream & ">Oui [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""disable""" & selected_disable_stream & ">Non</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=CENTER COLSPAN=2><BR><CENTER><H2>Lecture des vidéos :</H2></CENTER></TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT HEIGHT=40 WIDTH=380>Ignorer les vidéos excédant une heure&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD HEIGHT=40 WIDTH=*>")
            patternpage.AppendLine("	  <SELECT NAME=""hidelongvids"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""yes""" & selected_no_long_vids & ">Oui [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""no""" & selected_long_vids & ">Non</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Format vidéo et codec utilisés&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""codec"" WIDTH=300>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Formats Microsoft :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""avi_mpeg4""" & selected_avi_mpeg4 & ">AVI (MPEG-4, MP3) [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""avi_msvideo1""" & selected_avi_msvideo1 & ">AVI (MSVideo1, PCM)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""avi_cinepak""" & selected_avi_cinepak & ">AVI (Cinepak, PCM)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""xvid""" & selected_xvid & ">AVI (Xvid, MP3)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""avi_mjpeg""" & selected_avi_mjpeg & ">AVI (MJPEG, PCM)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""avi_yuv""" & selected_avi_yuv & ">AVI (YUV, PCM) [!]</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""wmv1""" & selected_oldwmv & ">WMV (WMV1, WMAv1)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""wmv2""" & selected_wmv & ">WMV (WMV2, WMAv2)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Formats Apple QuickTime :</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""mov_cinepak""" & selected_mov_cinepak & ">MOV (Cinepak, PCM)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""mov_mpeg4""" & selected_mov_mpeg4 & ">MOV (MPEG-4, MP2)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""mov_rpza""" & selected_mov_rpza & ">MOV (RPZA, PCM)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""mov_svq1""" & selected_mov_svq1 & ">MOV (Sorenson SVQ1, MP3)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""mov_mjpeg""" & selected_mov_mjpeg & ">MOV (MJPEG, PCM)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Autres formats universels ou génériques :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""mp4""" & selected_mp4 & ">MP4 (H.264, AAC)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""legacy_mp4""" & selected_mp4_legacy & ">MP4 (Pour vieux lecteurs)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""mpeg1""" & selected_mpg & ">MPEG (MPEG-1 100% compatible, MP2)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""recent_mpeg1""" & selected_mpg & ">MPEG (MPEG-1, MP2)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""rm""" & selected_rm & ">Real Media (RV10, AC3)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""3gp""" & selected_3gp & ">3GP (H.263, AMR-NB)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Formats Flash Player :</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""flv""" & selected_flv & ">Macromedia Flash (Sorenson Spark, MP3)</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Résolution de la vidéo&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""resolution"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""auto""" & selected_autosize & ">Automatique [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""96p""" & selected_96p & ">96p (Minimale)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""120p""" & selected_120p & ">120p (Très Faible)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""144p""" & selected_144p & ">144p (Faible)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""240p""" & selected_240p & ">240p (Basse)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""360p""" & selected_360p & ">360p (Moyenne)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""480p""" & selected_480p & ">480p (Standard)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""720p""" & selected_720p & ">720p (Haute) [HD]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""1080p""" & selected_1080p & ">1080p (Très Haute) [HD]</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Nombre d'images par seconde&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""framerate"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""auto""" & selected_framerate10 & ">Automatique [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""10""" & selected_framerate10 & ">10 images</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""12""" & selected_framerate12 & ">12 images</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""15""" & selected_framerate15 & ">15 images</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""20""" & selected_framerate20 & ">20 images</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""24""" & selected_framerate24 & ">24 images</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""25""" & selected_framerate25 & ">25 images</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""30""" & selected_framerate30 & ">30 images</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""60""" & selected_framerate60 & ">60 images (Pour PC récents)</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Intégration multimédia utilisée&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""player"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""no_integration""" & selected_nointegration & ">(Aucune intégration)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Lecteurs propriétaires et open source :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""legacy_wmp""" & selected_legacy_wmp & ">Windows Media Player 6.4 (ActiveX)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""wmp""" & selected_wmp & ">Windows Media Player 7.0 et plus (ActiveX)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""quicktime""" & selected_quicktime & ">Lecteur Apple QuickTime (ActiveX)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""embed_quicktime""" & selected_embed_quick & ">Lecteur Apple QuickTime (Embarqué)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vlc""" & selected_vlc & ">Lecteur VLC (ActiveX)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""alt_vlc""" & selected_altvlc & ">Lecteur VLC (Alt. ActiveX)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""embed_vlc""" & selected_vlcembed & ">Lecteur VLC (Embarqué)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""activex_realplayer""" & selected_realplayer_activex & ">Lecteur Real Player (ActiveX)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""realplayer""" & selected_realplayer & ">Lecteur Real Player (Embarqué)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Intégration via Flash Player :</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""flash""" & selected_flashplayer & ">Lecteur Flash Player (Javascript)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""embed_flash""" & selected_embedflash & ">Lecteur Flash Player (Embarqué)</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""activex_flash""" & selected_objectflash & ">Lecteur Flash Player (ActiveX)</OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Intégrations génériques et HTML5 :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""embed""" & selected_embed & ">Intégration Générique (Embarquée) [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""object""" & selected_genobject & ">Intégration Générique (Object)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""video""" & selected_video & ">Intégration Vidéo HTML5 Standard</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""alt_video""" & selected_alt_video & ">Vidéo HTML5 (Android, Nintendo, PlayStation)</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=CENTER COLSPAN=2><BR><CENTER><H2>Apparence du site :</H2></CENTER></TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Taille du lecteur multimédia intégré&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""size"" WIDTH=300>")
            If (Not old_ie) Then patternpage.AppendLine("	 	   <OPTION DISABLED>Proportions 4:3 :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""micro""" & selected_micro & ">Micro (160x140)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""ultrasmall""" & selected_ultrasmall & ">Ultra Compact (256x192)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""small""" & selected_small & ">Compact (320x240)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""cs""" & selected_classic_size & ">Classique (480x360)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""middle""" & selected_middle & ">Standard (640x480) [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""eh""" & selected_eh & ">Moyen (800x600)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""ot""" & selected_ot & ">Grand (1024x768)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""otsh""" & selected_otsh & ">Très grand (1600x1200)</OPTION>")

            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Proportions 16:9 :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""large""" & selected_large & ">Petit cinéma 16:9 (854x480)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""cinema""" & selected_cinema & ">Cinéma standard 16:9 (1280x720)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""bigcinema""" & selected_big_cinema & ">Cinéma grand format 16:9 (2560x1440)</OPTION>")

            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Proportions 16:10 :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""gold1""" & selected_gold1 & ">Standard en 16:10 (1280x800)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""gold2""" & selected_gold2 & ">Grand format en 16:10 (1440x900)</OPTION>")

            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Proportions verticales :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vertical1""" & selected_vertical1 & ">Vertical classique en 3:4 (270x480)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vertical2""" & selected_vertical2 & ">Vertical standard en 9:16 (360x640)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vertical3""" & selected_vertical3 & ">Vertical grand en 9:16 (720x1280)</OPTION>")

            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED></OPTION>")
            If (Not old_ie) Then patternpage.AppendLine("	   <OPTION DISABLED>Proportions dynamiques :</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""auto""" & selected_auto & ">Automatique (Avec Javascript)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""autoheight""" & selected_auto_height & ">Automatique (Selon taille vidéo)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""fullscreen""" & selected_fullscreen & ">Plein écran (Avec HTML)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""fulljs""" & selected_fulljs & ">Plein écran (Avec Javascript)</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Taille du lecteur pour les shorts&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""vsize"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""vert0""" & selected_v0 & ">Taille verticale micro (144x256)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vert1""" & selected_v1 & ">Taille verticale petite (270x480) [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vert2""" & selected_v2 & ">Taille verticale moyenne (360x640)</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""vert3""" & selected_v3 & ">Grande taille verticale (720x1280)</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")
            patternpage.AppendLine()

            patternpage.AppendLine("	<TR>")
            patternpage.AppendLine("	 <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Thème utilisé pour le site&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine()

            patternpage.AppendLine("	 <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("	  <SELECT NAME=""skin"" WIDTH=300>")
            patternpage.AppendLine("	   <OPTION VALUE=""oldyt""" & selected_classic & ">Classic</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""cosmic""" & selected_cosmic & ">Cosmic Tube [Par défaut]</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""modern""" & selected_modern & ">Modern</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""dark""" & selected_dark & ">Dark Mode</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""rose""" & selected_rose & ">Rose</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""aqua""" & selected_aqua & ">Aqua</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""mint""" & selected_mint & ">Mint</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""sunshine""" & selected_sunshine & ">Sunshine</OPTION>")
            patternpage.AppendLine("	   <OPTION VALUE=""monochrome""" & selected_monochrome & ">Monochrome</OPTION>")
            patternpage.AppendLine("	  </SELECT>")
            patternpage.AppendLine("	 </TD>")
            patternpage.AppendLine("	</TR>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Afficher le volet droit (suggestions, playlists, shorts)&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("     <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("      <SELECT NAME=""panel"" WIDTH=300>")
            patternpage.AppendLine("       <OPTION VALUE=""true""" & selected_panel & ">Activé [Par défaut]</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""false""" & selected_nopanel & ">Désactivé</OPTION>")
            patternpage.AppendLine("      </SELECT>")
            patternpage.AppendLine("     </TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Affichage des commentaires sous les vidéos&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("     <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("      <SELECT NAME=""displaycomments"" WIDTH=300>")
            patternpage.AppendLine("       <OPTION VALUE=""0""" & selected_disp_zero & ">Ne pas afficher les commentaires</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""10""" & selected_disp_ten & ">Afficher 10 commentaires</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""20""" & selected_disp_twenty & ">Afficher 20 commentaires [Par défaut]</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""50""" & selected_disp_fifty & ">Afficher 50 commentaires</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""100""" & selected_disp_hundred & ">Afficher 100 commentaires</OPTION>")
            patternpage.AppendLine("      </SELECT>")
            patternpage.AppendLine("     </TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("    <TR>")
            patternpage.AppendLine("     <TD ALIGN=RIGHT WIDTH=380 HEIGHT=40>Nombre de vidéos affichées par page dans les canaux&nbsp;:&nbsp;</TD>")
            patternpage.AppendLine("     <TD WIDTH=* HEIGHT=40>")
            patternpage.AppendLine("      <SELECT NAME=""vcn"" WIDTH=300>")
            patternpage.AppendLine("       <OPTION VALUE=""9""" & selected_channel_nine & ">9 vidéos</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""18""" & selected_channel_eighteen & ">18 vidéos [Par défaut]</OPTION>")
            patternpage.AppendLine("       <OPTION VALUE=""27""" & selected_channel_twentyseven & ">27 vidéos</OPTION>")
            patternpage.AppendLine("      </SELECT>")
            patternpage.AppendLine("     </TD>")
            patternpage.AppendLine("    </TR>")

            patternpage.AppendLine("   </TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("   <CENTER><P>Cliquez sur le bouton pour <INPUT TYPE=""SUBMIT"" VALUE=""Enregistrer"" CLASS=""red_button"" /> la configuration, ou sur ce lien pour <A HREF=""/resetcfg.cgi"">réinitialiser les paramètres</A>.</P></CENTER>")
            patternpage.AppendLine("  </FORM><BR><BR>")
            patternpage.AppendLine("  <NOSCRIPT><P ALIGN=CENTER><B>Avertissement:</B>&nbsp;Javascript semble indisponible sur votre navigateur. Veuillez le réactiver ou changer de navigateur, si vous voulez utiliser certaines options.<BR><BR></P></NOSCRIPT>")
            patternpage.AppendLine("  <CENTER><DIV STYLE=""display: block; width: 780px; margin-left: auto; margin-right: auto; !text-align: center;""><P STYLE=""text-align: justify;""><B>NOTA:</B>&nbsp;L'intégration du lecteur multimédia HTML5 convient aux navigateurs publiés après l'année 2008. Auquel cas, il est également recommandé de faire usage du format MP4. Une version de cette intégration, adaptée aux anciennes versions d'Android, aux consoles Nintendo ou SONY, permet de rendre la lecture plus aisée sur ces périphériques connectables à Internet. Nintendo® et SONY® sont des marques déposées appartenant à leurs sociétés respectives (Merci à LeJarb pour son code d'intégration spécifique à ces configurations).<BR><BR>")
            patternpage.AppendLine("  <B>NOTA 2:</B>&nbsp;Par ailleurs, le mode 60 FPS n'est pas tout le temps disponible pour la lecture. Il faut que la vidéo source soit déjà en 60 FPS, ce qui n'est pas toujours le cas. Le format de destination doit être assez récent pour prendre en charge ce nombre d'images par seconde. Prudence sur les anciennes configurations, où un tel nombre d'images par seconde peut provoquer des saturations mémoire.</P>")
            patternpage.AppendLine("  <VIDEO STYLE=""height: 32px; width: 780px;""><P ALIGN=CENTER><B>NOTA 3:</B>&nbsp;Votre navigateur ne semble pas supporter le HTML5. Il est donc déconseillé d'utiliser l'intégration de type vidéo HTML5 pour lire du contenu multimédia.</VIDEO></DIV></CENTER>")
            patternpage.AppendLine(" <BR><BR><BR><BR>" & footer)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("POST /savecfg.cgi") Then
            'Sauvegarde de la configuration client
            Dim rqcontent As String = request.Remove(0, request.IndexOf(vbCrLf & vbCrLf) + 4)
            rqcontent = rqcontent.Trim()
            rqcontent = rqcontent.Replace(Chr(10), String.Empty)
            rqcontent = rqcontent.Replace(Chr(13), String.Empty)

            If Not String.IsNullOrEmpty(rqcontent) Then
                'Pour éviter des injections d'entêtes HTTP
                If rqcontent.Contains(vbCrLf) Then
                    rqcontent = rqcontent.Substring(0, rqcontent.IndexOf(vbCrLf))
                End If

                If rqcontent.Length > 1024 Then 'Limiter le cookie à 1Ko
                    rqcontent = rqcontent.Substring(0, 1024)
                End If
            End If

            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>La configuration a été sauvegardée. Vous pouvez maintenant retourner à la section des <A HREF=""/config.cgi"">paramètres</A>.</P>" & vbCrLf

            Dim index_resp As String =
                "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf

            Dim exp As String = DateTime.UtcNow.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", Globalization.CultureInfo.InvariantCulture)

            If Not String.IsNullOrEmpty(rqcontent) Then
                index_resp &= "Set-Cookie: " & cookie_header & rqcontent & "; Path=/; Expires=" & exp & vbCrLf 'L'ajout de la variable path garantit l'usage du cookie sur tout le domaine (compatibilité IE6 et assimilés)
            End If

            index_resp &= "Location: /config.cgi?message=gotsaved" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message avec lien si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                client.Close()
                Exit Sub
            End Try

            If Not String.IsNullOrEmpty(rqcontent) Then WriteLog("Nouveau cookie pour le client: " & cookie_header & rqcontent & "; Path=/; Expires=" & exp, ConsoleColor.Yellow, client)
            client.Close()

        ElseIf request.Contains("GET /savecfg.cgi") Then
            'Message d'erreur requête vide
            WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Ce formulaire ne peut être utilisé par la requête GET. Veuillez passer par <A HREF=""/config.cgi"">la section paramètres</A> pour changer les paramètres du client.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /resetcfg.cgi") Then
            'Réinitialiser la configuration client

            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>La configuration a été réinitialisée, vous pouvez maintenant naviguer sur <A HREF=""/config.cgi"">cette page</A>.</P>" & vbCrLf

            Dim exp As String = DateTime.UtcNow.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", Globalization.CultureInfo.InvariantCulture)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Set-Cookie: " & cookie_header & "results=10&size=middle&codec=recent_mpeg1&player=embed&skin=cosmic&resolution=auto&framerate=auto&panel=true&displaycomments=20&vcn=18&trends=enable&displaystream=enable&hidelongvids=yes&vsize=vert1; Path=/; Expires=" & exp & vbCrLf &
                "Location: /config.cgi&message=gotreset" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
            End Try

            WriteLog("Configuration réinitialisée pour le client.", ConsoleColor.Yellow, client)
            client.Close()
        ElseIf request.StartsWith("GET /com.cgi?v=") Then
            'Lister les commentaires sur une page à part.
            Dim arg1 As String = request.Split(" ")(1)
            Dim last_view As String = String.Empty
            Dim com_offset As Integer = 0
            Dim has_error As Boolean = False
            Dim dcpv As Integer = Math.Max(20, disp_comments_per_video)
            Dim is_short As Boolean = False

            Try
                arg1 = arg1.Remove(0, 11)
                If String.IsNullOrEmpty(arg1) OrElse arg1.Length = 0 Then
                    has_error = True
                Else
                    arg1 = arg1.Trim
                    arg1 = arg1.Replace(vbLf, String.Empty)
                    arg1 = arg1.Replace(vbCr, String.Empty)
                End If

                If arg1.Contains("&type=short") Then
                    arg1 = arg1.Replace("&type=short", String.Empty)
                    is_short = True
                End If

                If arg1.EndsWith("&") Then
                    arg1 = arg1.Remove(arg1.Length - 1, 1)
                ElseIf arg1.Contains("&") Then
                    Dim tmp_arg As String = arg1
                    arg1 = arg1.Substring(0, arg1.IndexOf("&"))
                    com_offset = CInt(CStr(tmp_arg.Substring(tmp_arg.IndexOf("&") + 1, tmp_arg.Length - tmp_arg.IndexOf("&") - 1)).Replace("page=", String.Empty))
                    com_offset -= 1
                End If
            Catch ex As Exception
                has_error = True
            End Try

            If has_error Then
                WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

                Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Il n'y a aucun paramètre indiqué sur la vidéo en cours de lecture.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub
            End If

            last_view = arg1

            If com_offset < 0 Then com_offset = 0

            If Not LooksLikeYoutubeID(last_view) Then
                WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

                Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>L'identifiant spécifié de la vidéo semble invalide. Veuillez réessayer avec un bon identifiant vidéo.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub
            End If

            WriteLog("Affichage des commentaires de la vidéo " & last_view & IIf(com_offset = 0, String.Empty, ", page " & CStr(com_offset + 1)) & "...", , client)

            If Not video_props.ContainsKey(last_view) Then
                WriteLog("Erreur HTTP #500: Erreur interne du serveur.", , client)

                Dim baddata As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Impossible de consulter les commentaires vidéo, car elle n'est pas (ou plus) en cache.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub
            End If

            If IO.File.Exists(CurDir() & "\comments\" & GetMD5(last_view) & ".json") AndAlso FileLen(CurDir() & "\comments\" & GetMD5(last_view) & ".json") > 6 Then
                WriteLog("Lecture du fichier JSON contenant les commentaires de la vidéo...")
                patternpage.AppendLine(InitValues("Commentaires sur " & video_props(last_view).Title, , wanted_skin, , used_player))
                Dim output_comments As String = IO.File.ReadAllText(CurDir() & "\comments\" & GetMD5(last_view) & ".json")
                Dim acc_coms As String = String.Empty
                Dim cid1, cid2 As Integer
                Dim total_comments As Integer = 0
                cid1 = 0
                cid2 = 0

                Do
                    cid1 = output_comments.IndexOf("{""id"":", cid2)
                    If cid1 = -1 Then Exit Do
                    cid2 = output_comments.IndexOf("}", cid1)
                    If cid1 >= cid2 Or cid1 = -1 Then Exit Do
                    total_comments += 1

                    If total_comments > com_offset * disp_comments_per_video AndAlso total_comments <= com_offset * disp_comments_per_video + dcpv Then
                        Dim one_comment As String = output_comments.Substring(cid1, cid2 - cid1)
                        one_comment = one_comment.Replace("\""", "&quot;")
                        Dim com_author As String = "(Auteur inconnu)"
                        Dim com_content As String = "(Contenu indisponible)"
                        Dim com_date As String = "(Date inconnue)"
                        Dim com_likes As String = "0"
                        Dim com_channel As String = "about:blank"

                        Dim param1, param2 As Integer

                        'Trouver l'auteur
                        param1 = one_comment.IndexOf("""author"": ""@")
                        If param1 >= 0 Then
                            param2 = one_comment.IndexOf("""", param1 + 12)
                            com_author = one_comment.Substring(param1 + 11, param2 - param1 - 11)
                        End If

                        param1 = one_comment.IndexOf("""text"": """)
                        If param1 >= 0 Then
                            param2 = one_comment.IndexOf("""", param1 + 9)
                            com_content = one_comment.Substring(param1 + 9, param2 - param1 - 9)
                            com_content = UnicodeJson(com_content)
                        End If

                        param1 = one_comment.IndexOf("""_time_text"": """)
                        If param1 >= 0 Then
                            param2 = one_comment.IndexOf("""", param1 + 15)
                            com_date = one_comment.Substring(param1 + 15, param2 - param1 - 15)
                            com_date = com_date.Replace(" ago", String.Empty)
                            com_date = com_date.Replace("years", "ans")
                            com_date = com_date.Replace("year", "an")
                            com_date = com_date.Replace("days", "jours")
                            com_date = com_date.Replace("day", "jour")
                            com_date = com_date.Replace("months", "mois")
                            com_date = com_date.Replace("month", "mois")
                            com_date = com_date.Replace("hours", "heures")
                            com_date = com_date.Replace("hour", "heure")
                            com_date = com_date.Replace("weeks", "semaines")
                            com_date = com_date.Replace("week", "semaine")
                            com_date = com_date.Replace("(edited)", "(modifié)")
                            com_date = "il y a " & com_date
                        End If

                        param1 = one_comment.IndexOf("""like_count"": """)
                        If param1 > -1 Then
                            param2 = one_comment.IndexOf(",", param1 + 14)
                            com_likes = one_comment.Substring(param1 + 14, param2 - param1 - 14)
                        End If

                        param1 = one_comment.IndexOf("""author_id"": """)
                        If param1 > -1 Then
                            param2 = one_comment.IndexOf("""", param1 + 14)
                            com_channel = "/channel.cgi?id=" & one_comment.Substring(param1 + 14, param2 - param1 - 14) & "&amp;section=videos"
                        End If

                        'acc_coms &= "<HR WIDTH=100% />" & vbCrLf
                        acc_coms &= "<P><B>Par <A HREF=""" & com_channel & """ STYLE=""color: " & link_color & """>" & com_author & "</A>, " & com_date & " :</B><BR>" & vbCrLf
                        acc_coms &= com_content & vbCrLf
                        If com_likes <> "0" Then
                            acc_coms &= "<B><IMG SRC=""th_up.gif"" ALT=""Pouce vert"" /><FONT COLOR=GREEN>" & com_likes & " utilisateur(s) ont aimé ce message.</FONT></B>" & vbCrLf
                        End If
                        acc_coms &= "</P><BR>" & vbCrLf & vbCrLf
                    End If
                Loop

                Dim prefix As String = "watch"
                If is_short Then prefix = "short"

                If String.IsNullOrEmpty(acc_coms) Then
                    patternpage.AppendLine("<BR><BR><CENTER><H2>Affichage des commentaires pour la vidéo <A HREF=""/" & prefix & "?v=" & last_view & """>" & video_props(last_view).Title & "</A> :<BR><BR>Aucun commentaire trouvé, ou numéro de page invalide.</H2></CENTER><DIV CLASS=bodysep></DIV>")
                Else
                    patternpage.AppendLine("<DIV STYLE=""width: 100%; padding: 24px 24px 24px 24px;""><CENTER><H2>" & total_comments.ToString & " commentaire(s) pour la vidéo <A HREF=""/" & prefix & "?v=" & last_view & """>" & video_props(last_view).Title & "</A> (Affichage des commentaires " & CStr(1 + com_offset * disp_comments_per_video) & " à " & CStr(Math.Min(total_comments, (com_offset * disp_comments_per_video) + dcpv)) & ") :</H2></CENTER><BR><BR>")
                    patternpage.AppendLine(acc_coms & "</DIV>")

                    If total_comments > dcpv Then
                        patternpage.AppendLine("<CENTER><P>")

                        Dim max_page As Integer = CInt(Math.Ceiling(CDbl(total_comments) / CDbl(disp_comments_per_video)))

                        patternpage.AppendLine("<FORM METHOD=""GET"" ACTION=""/com.cgi"">")

                        If com_offset <> 0 Then
                            patternpage.Append("<LABEL><A HREF=""/com.cgi?v=" & last_view & "&amp;page=" & CStr(CInt(com_offset)) & IIf(is_short, "&type=short", String.Empty) & """>&lt; Page précédente</A></LABEL>&nbsp;|&nbsp;")
                        Else
                            patternpage.Append("<LABEL>&lt; Page précédente</LABEL>&nbsp;|&nbsp;")
                        End If

                        If com_offset <> max_page - 1 Then
                            patternpage.Append("<LABEL><A HREF=""/com.cgi?v=" & arg1 & "&amp;page=" & CStr(CInt(com_offset + 2)) & IIf(is_short, "&type=short", String.Empty) & """>Page suivante &gt;</A></LABEL>&nbsp;|&nbsp;")
                        Else
                            patternpage.AppendLine("<LABEL>Page suivante &gt;</LABEL>&nbsp;|&nbsp;")
                        End If

                        patternpage.AppendLine("<LABEL>Page " & CInt(com_offset + 1).ToString & " sur " & max_page.ToString & "&nbsp;|&nbsp;Aller à la page: </LABEL>")
                        patternpage.AppendLine(" <INPUT NAME=""v"" TYPE=""hidden"" VALUE=""" & last_view & """ />")
                        patternpage.AppendLine(" <INPUT NAME=""page"" VALUE=""1"" MAXLENGTH=12 SIZE=3 />")
                        patternpage.AppendLine(" <INPUT TYPE=""submit"" VALUE=""OK"" CLASS=""red_button"" STYLE=""width: 32px;"" />")
                        patternpage.AppendLine("</FORM>")
                        patternpage.AppendLine("</P></CENTER><BR><BR>")
                    End If
                End If

                patternpage.AppendLine(footer)

                WriteLog("Il y a au total " & total_comments.ToString & " commentaire(s) trouvé(s) sur cette vidéo.", ConsoleColor.Blue)

                Dim index_resp As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Cache-Control: no-cache, no-store, must-revalidate" & vbCrLf &
                "Pragma: no-cache" & vbCrLf &
                "Expires: 0" & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                Dim comdata As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(comdata, 0, comdata.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub
            Else
                WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

                Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La vidéo n'est pas en cache. Les commentaires ne sont donc pas accessibles.<BR><BR>Veuillez vous rendre sur la <A HREF=""/watch?v=" & last_view & """>page de lecture</A> d'abord.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub
            End If
        ElseIf request.StartsWith("GET /com.cgi") Then
            'Les autres requêtes entraînent une erreur 400 (requête invalide).
            WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Vous devez spécifier la vidéo sur laquelle consulter les commentaires en paramètre (avec <I>v=id_video</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                client.Close()
                Exit Sub
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /v/") Then
            'Récupération directe du fichier vidéo
            Dim arg1 As String = request.Remove(0, 7) 'Retirer l'entête GET /v/
            arg1 = arg1.Replace("../", String.Empty)
            arg1 = arg1.Replace("/..", String.Empty)
            arg1 = arg1.Replace("./", String.Empty)
            arg1 = arg1.Replace("/.", String.Empty)

            arg1 = arg1.Replace("..\", String.Empty)
            arg1 = arg1.Replace("\..", String.Empty)
            arg1 = arg1.Replace(".\", String.Empty)
            arg1 = arg1.Replace("\.", String.Empty)

            arg1 = arg1.Substring(0, arg1.IndexOf(" "))
            If arg1.Contains("&") Then
                arg1 = arg1.Substring(0, arg1.IndexOf("&"))
            End If

            If Not IO.File.Exists(CurDir() & "\vidcache\" & arg1) Then
                Dim notfound_data As Byte()
                notfound_data = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>La vidéo avec pour nom de fichier '" & arg1.Replace(">", "&gt;").Replace("<", "&lt;") & "' n'a pas été trouvée sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                client.Close()
                WriteLog("Le nom de fichier vidéo suivant est introuvable: '" & arg1 & "'.")
            Else
                Dim media_type As String = "application/octet-stream"

                Select Case used_codec
                    Case "mp4", "legacy_mp4" : media_type = "video/mp4"
                    Case "rm" : media_type = "application/vnd.rn-realmedia"
                    Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak", "xvid" : media_type = "video/x-msvideo"
                    Case "wmv1", "wmv2" : media_type = "video/x-ms-wmv"
                    Case "mov_cinepak", "mov_svq1", "mov_mpeg4", "mov_rpza" : media_type = "video/quicktime"
                    Case "mpeg1", "recent_mpeg1" : media_type = "video/mpeg"
                    Case "3gp" : media_type = "video/3gpp"
                    Case "flv" : media_type = "video/x-flv"
                    Case Else : media_type = "application/octet-stream"
                End Select

                Dim sent_output_data As Byte()
                Dim sent_output_res As String = String.Empty
                Dim f_length As Long = FileLen(CurDir() & "\vidcache\" & arg1)

                If range_begin = -2 Then
                    range_begin = f_length - range_end - 1
                    range_end = f_length - 1
                End If

                If range_end = -2 Then
                    range_end = f_length - 1
                End If

                If range_begin >= 0 Or range_end >= 0 Then
                    If range_begin >= f_length Or range_end > f_length Or range_begin < 0 Or range_end < 0 Then
                        'Lever une erreur
                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                        Try
                            stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                        Catch ey As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                        End Try

                        client.Close()
                        Exit Sub
                    Else
                        'Seule une partie du fichier vidéo est demandée
                        sent_output_res = "HTTP/" & http_ver & " 206 Partial Content" & vbCrLf
                        sent_output_res &= "Content-Type: " & media_type & vbCrLf
                        sent_output_res &= "Connection: close" & vbCrLf
                        sent_output_res &= "Content-Range: bytes " & range_begin.ToString & "-" & range_end.ToString & "/" & f_length.ToString & vbCrLf
                        sent_output_res &= "Content-Length: " & CStr(range_end - range_begin + 1) & vbCrLf
                        sent_output_res &= "Accept-Ranges: bytes" & vbCrLf & vbCrLf
                        sent_output_data = iso.GetBytes(sent_output_res)
                    End If
                Else
                    sent_output_res = "HTTP/" & http_ver & " 200 OK" & vbCrLf
                    sent_output_res &= "Content-Type: " & media_type & vbCrLf
                    sent_output_res &= "Connection: close" & vbCrLf
                    sent_output_res &= "Content-Length: " & f_length.ToString & vbCrLf
                    sent_output_res &= "Accept-Ranges: bytes" & vbCrLf & vbCrLf
                    sent_output_data = iso.GetBytes(sent_output_res)
                End If

                Try
                    stream.Write(sent_output_data, 0, sent_output_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                Dim fs_output As FileStream = Nothing
                Dim resBuffer_output(8191) As Byte
                Dim resread_output As Integer
                Dim total_sent As Long = 0
                Dim percent As Integer = 0

                If range_begin >= 0 Or range_end >= 0 Then
                    fs_output.Seek(range_begin, SeekOrigin.Begin)
                End If

                Try
                    fs_output = New FileStream(CurDir() & "\vidcache\" & arg1, FileMode.Open, FileAccess.Read)

                    Do
                        resread_output = fs_output.Read(resBuffer_output, 0, resBuffer_output.Length)
                        If resread_output <= 0 Then Exit Do
                        stream.Write(resBuffer_output, 0, resread_output)
                        total_sent += resread_output

                        If total_sent > range_end And range_end >= 0 Then
                            Exit Do
                        End If

                        percent = CInt((total_sent / fs_output.Length) * 100)
                        Console.WriteLine("Transfert en cours, veuillez patienter. " & percent.ToString & "% effectués...          ")

                        Try
                            Console.SetCursorPosition(0, Console.CursorTop - 1)
                        Catch ex As Exception

                        End Try
                    Loop

                    WriteLog("Fichier vidéo '" & arg1 & "' envoyé au client avec succès.", ConsoleColor.Green, client)
                Catch ex As IOException
                    percent = 0
                    If fs_output IsNot Nothing Then percent = CInt((total_sent / fs_output.Length) * 100)
                    WriteLog("Transfert du fichier vidéo '" & arg1 & "' interrompu à " & percent.ToString & "%, car: " & ex.Message, ConsoleColor.Yellow, client)
                Catch ex As Exception
                    WriteLog("Erreur lors du transfert du fichier vidéo '" & arg1 & "': " & ex.Message, ConsoleColor.Red, client)
                Finally
                    If fs_output IsNot Nothing Then
                        fs_output.Close()
                    End If
                    client.Close()
                End Try
            End If
        ElseIf request.StartsWith("GET /channel.cgi?id=") Then
            If number_of_channels > 10 Then
                Dim baddata As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes simultanées</H1>" & vbCrLf & "<P>Trop de chaînes sont en cours de lecture par le serveur. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
                Exit Sub
            End If

            'Afficher une chaîne
            Dim arg1 As String = Nothing
            Dim vid_offset As Integer = 0
            Dim error_encountered As Boolean = False
            Dim vid_section As String = "videos"

            Try
                arg1 = request.Split(" ")(1)
                arg1 = arg1.Remove(0, 16)
            Catch ex As Exception
                error_encountered = True
            End Try

            If error_encountered OrElse String.IsNullOrEmpty(arg1) OrElse arg1.Length = 0 Then
                WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

                Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Impossible d'afficher la chaîne demandée, car aucun identifiant de chaîne n'a été spécifié.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                WriteLog("Une chaîne utilisateur a été demandée, mais aucun identifiant n'a été communiqué.", ConsoleColor.Red, client)

                client.Close()
            Else
                number_of_channels += 1

                Dim url_params() As String

                If arg1.EndsWith("&") Then
                    arg1 = arg1.Remove(arg1.Length - 1, 1)
                End If

                If arg1.Contains("&") Then
                    Dim tmp_arg As String = arg1
                    arg1 = arg1.Substring(0, arg1.IndexOf("&")) 'Prendre l'identifiant de la chaîne elle-même
                    tmp_arg = tmp_arg.Remove(0, tmp_arg.IndexOf("&") + 1)

                    If tmp_arg.Contains("=") Then
                        url_params = tmp_arg.Split("&")

                        For Each u As String In url_params
                            Dim sub_params() As String = u.Split("=")
                            Select Case sub_params(0)
                                Case "section"
                                    If {"videos", "shorts", "playlists", "streams"}.Contains(sub_params(1)) Then
                                        vid_section = sub_params(1)
                                    End If
                                Case "page"
                                    If IsNumeric(sub_params(1)) Then
                                        Try
                                            vid_offset = CInt(sub_params(1)) - 1
                                        Catch ex As Exception
                                            vid_offset = 0
                                        End Try
                                    End If
                            End Select
                        Next
                    End If
                End If

                If vid_offset < 0 Then vid_offset = 0 'Ramener à zéro

                WriteLog("Demande d'informations sur la chaîne " & arg1 & IIf(vid_offset = 0, String.Empty, ", page " & CStr(vid_offset + 1)) & ", section '" & vid_section & "'.", ConsoleColor.Blue, client)

                Dim op_get_channel As OutputResponse = LaunchProcess("-J --playlist-items 1 ""https://www.youtube.com/channel/" & arg1 & "/""") '60000 ms
                Dim output4 As String = op_get_channel.OutputData
                Dim err4 As String = op_get_channel.ErrorData

                Dim channel_name As String = "&lt;Nom inconnu&gt;"
                Dim channel_desc As String = "&lt;Aucune description disponible&gt;"
                Dim channel_followers As String = "0"
                Dim channel_num_vids As Integer = 0
                Dim channel_banner As String = "about:blank" '"thumbnails": [{"url": " et "
                Dim channel_upid As String = "@unknown"
                Dim channel_avatar As String = "about:blank"

                If String.IsNullOrEmpty(output4) OrElse output4.StartsWith("null") Then
                    Dim baddata As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La chaîne demandée n'existe pas selon les serveurs YouTube, ou ne contient aucune information traitable.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                    Try
                        stream.Write(baddata, 0, baddata.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        number_of_channels -= 1
                        client.Close()
                        Exit Sub
                    End Try

                    number_of_channels -= 1
                    client.Close()
                    Exit Sub
                End If

                Dim param1, param2 As Integer

                output4 = output4.Replace("\""", "&quot;")

                param1 = output4.IndexOf("""channel"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 12)
                    channel_name = output4.Substring(param1 + 12, param2 - param1 - 12)
                End If

                param1 = output4.IndexOf("""description"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 16)
                    channel_desc = output4.Substring(param1 + 16, param2 - param1 - 16)
                    channel_desc = channel_desc.Replace("&quot;", """")
                    channel_desc = EscapeHtml(channel_desc)
                End If

                param1 = output4.IndexOf("""channel_follower_count"": ")
                If param1 > -1 Then
                    param2 = output4.IndexOf(",", param1 + 26)
                    channel_followers = output4.Substring(param1 + 26, param2 - param1 - 26).Trim
                    If Not IsNumeric(channel_followers) Then channel_followers = "Nombre inconnu d'"
                End If

                param1 = output4.IndexOf("""thumbnails"": [{""url"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 24)
                    channel_banner = output4.Substring(param1 + 24, param2 - param1 - 24)
                End If

                param1 = output4.IndexOf("""uploader_id"": ""@")
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 17)
                    channel_upid = output4.Substring(param1 + 17, param2 - param1 - 17)
                End If

                If output4.Contains("{""url"": """) Then
                    Dim url1, url2, url3 As Integer
                    Dim suburl As String = String.Empty
                    url1 = 0
                    url2 = 0
                    url3 = 0

                    Do
                        url1 = output4.IndexOf("{""url"": """, url2 + 1)
                        If url1 = -1 Then Exit Do
                        url2 = output4.IndexOf("}", url1 + 9)
                        If url2 = -1 Then Exit Do

                        suburl = output4.Substring(url1, url2 - url1)
                        If suburl.Contains("""id"": ""avatar_uncropped"", ") Then
                            url3 = suburl.IndexOf("""", 10)
                            channel_avatar = suburl.Substring(9, url3 - 9)
                            Exit Do
                        End If
                    Loop
                End If

                'Liste des vidéos de la chaîne
                Dim op_play As OutputResponse = Nothing

                If vid_section = "playlists" Then
                    op_play = LaunchProcess("--print ""%()j"" --flat-playlist ""https://www.youtube.com/channel/" & arg1 & "/" & vid_section & "/""", , , , 600000)
                Else
                    op_play = LaunchProcess("--flat-playlist --print ""%(id)s<|>"" ""https://www.youtube.com/channel/" & arg1 & "/" & vid_section & "/""", , , , 600000)
                End If

                Dim output5 As String = op_play.OutputData
                output5 = output5.Trim(vbLf)
                output5 = output5.Trim(vbCr)
                If output5.EndsWith("<|>") Then output5 = output5.Remove(output5.Length - 3, 3)
                Dim err5 As String = op_play.ErrorData

                If String.IsNullOrEmpty(output5) OrElse output5.Length = 0 OrElse output5.StartsWith("null") Then

                    Dim final_channel_name2 As String = UnicodeJson(channel_name)
                    Dim first_channel_char2 As String = "x"

                    If final_channel_name2.Length > 0 Then
                        first_channel_char2 = final_channel_name2.Substring(0, 1).ToLower
                        Select Case first_channel_char2
                            Case "a", "e", "i", "o", "u", "y"
                                final_channel_name2 = "Chaîne d'" & final_channel_name2
                            Case "é", "è", "ë", "ê", "ä", "à", "ô", "ö", "ò", "ÿ", "ü", "û", "ù", "å", "ã", "ì", "í", "î", "ï", "õ", "ø", "ú", "ý"
                                final_channel_name2 = "Chaîne d'" & final_channel_name2
                            Case "h"
                                final_channel_name2 = "Chaîne d'" & final_channel_name2
                            Case Else
                                final_channel_name2 = "Chaîne de " & final_channel_name2
                        End Select
                    End If

                    patternpage.AppendLine(InitValues(final_channel_name2, , wanted_skin, , used_player))
                    patternpage.AppendLine("<CENTER><H1 CLASS=""black_label"">" & final_channel_name2 & "</H1></CENTER>")
                    If channel_banner <> "about:blank" And output4.Contains("""id"": ""banner_uncropped""") Then patternpage.AppendLine("<BR><CENTER><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(channel_banner) & "&amp;type=banner"" ALT=""Bannière de " & channel_name & """ WIDTH=600 HEIGHT=100 STYLE=""border-radius: 8px;"" /></CENTER>")
                    patternpage.AppendLine("<BR><CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=600>")
                    patternpage.AppendLine(" <TR>")
                    patternpage.AppendLine("  <TD WIDTH=92 VALIGN=TOP>")
                    patternpage.AppendLine("   <CENTER><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(channel_avatar) & "&amp;type=avatar"" ALT=""Avatar de " & channel_name & """ WIDTH=64 HEIGHT=64 STYLE=""border-radius: 32px;"" /></CENTER>")
                    patternpage.AppendLine("  </TD>")
                    patternpage.AppendLine("  <TD WIDTH=*>")

                    Select Case vid_section
                        Case "shorts"
                            patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; 0 short</B></P>")
                        Case "playlists"
                            patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; 0 playlist</B></P>")
                        Case "streams"
                            patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; 0 vidéo en live</B></P>")
                        Case Else
                            patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; 0 vidéo</B></P>")
                    End Select

                    patternpage.AppendLine("   <P>" & UnicodeJson(channel_desc) & "</P><BR>")
                    patternpage.AppendLine("  </TD>")
                    patternpage.AppendLine(" </TR>")
                    patternpage.AppendLine(" <TR>")
                    patternpage.Append("  <TD COLSPAN=3><CENTER><H2 CLASS=""black_label""><B>Sections de la chaîne :</B></H1><BR>")

                    Select Case vid_section
                        Case "shorts"
                            patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <B>SHORTS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                        Case "playlists"
                            patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <B>PLAYLISTS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                        Case "streams"
                            patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <B>LIVES</B></CENTER></TD>")
                        Case Else
                            patternpage.AppendLine("<B>VID&Eacute;OS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                    End Select

                    patternpage.AppendLine(" </TR>")
                    patternpage.AppendLine("</TABLE></CENTER><BR>")
                    patternpage.AppendLine("<DIV CLASS=bodysep></DIV><CENTER><H2>Aucune vidéo trouvée dans le flux indiqué.</H2></CENTER><DIV CLASS=bodysep></DIV>")
                    patternpage.AppendLine(footer)

                    Dim index_err_resp As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                                           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                           "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                                           "Connection: close" & vbCrLf &
                                           "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                    Dim index_err_data As Byte() = iso.GetBytes(index_err_resp)

                    Try
                        stream.Write(index_err_data, 0, index_err_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try

                    number_of_channels -= 1
                    client.Close()
                    Exit Sub
                End If

                Dim vid_identifiers() As String = output5.Split("<|>")
                channel_num_vids = vid_identifiers.Count

                Dim final_channel_name As String = UnicodeJson(channel_name)
                Dim first_channel_char As String = "x"

                If final_channel_name.Length > 0 Then
                    first_channel_char = final_channel_name.Substring(0, 1).ToLower
                    Select Case first_channel_char
                        Case "a", "e", "i", "o", "u", "y"
                            final_channel_name = "Chaîne d'" & final_channel_name
                        Case "é", "è", "ë", "ê", "ä", "à", "ô", "ö", "ò", "ÿ", "ü", "û", "ù", "å", "ã", "ì", "í", "î", "ï", "õ", "ø", "ú", "ý"
                            final_channel_name = "Chaîne d'" & final_channel_name
                        Case "h"
                            final_channel_name = "Chaîne d'" & final_channel_name
                        Case Else
                            final_channel_name = "Chaîne de " & final_channel_name
                    End Select
                End If

                patternpage.AppendLine(InitValues(final_channel_name, , wanted_skin, , used_player))

                patternpage.AppendLine("<CENTER><H1 CLASS=""black_label"">" & final_channel_name & "</H1></CENTER>")
                If channel_banner <> "about:blank" And output4.Contains("""id"": ""banner_uncropped""") Then patternpage.AppendLine("<BR><CENTER><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(channel_banner) & "&amp;type=banner"" ALT=""Bannière de " & channel_name & """ WIDTH=600 HEIGHT=100 STYLE=""border-radius: 8px;"" /></CENTER>")
                patternpage.AppendLine("<BR><CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=600>")
                patternpage.AppendLine(" <TR>")
                patternpage.AppendLine("  <TD WIDTH=92 VALIGN=TOP>")
                patternpage.AppendLine("   <CENTER><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(channel_avatar) & "&amp;type=avatar"" ALT=""Avatar de " & channel_name & """ WIDTH=64 HEIGHT=64 STYLE=""border-radius: 32px;"" /></CENTER>")
                patternpage.AppendLine("  </TD>")
                patternpage.AppendLine("  <TD WIDTH=*>")

                Dim compiledpage As New StringBuilder
                Dim f_counter As Integer = 0

                If vid_section = "playlists" Then
                    output5 = output5.Replace("\""", "&quot;")
                    output5 = output5.Replace("<", "&lt;")
                    output5 = output5.Replace(">", "&gt;")

                    Dim found_elements() As String = Split(output5, "{""title"": """)

                    If found_elements.Length = 0 Then
                        compiledpage.AppendLine("<CENTER><H2>Aucune playlist n'a été trouvée sur cette chaîne !</H2></CENTER><DIV CLASS=bodysep></DIV>")
                    Else
                        Dim actually_found As Boolean = False

                        compiledpage.AppendLine("<CENTER><TABLE BORDER=0 WIDTH=400 CELLPADDING=8 CELLSPACING=0>")
                        For Each fe As String In found_elements
                            Dim found_playlist_title As String = "(Sans titre)"
                            Dim found_playlist_thumbnail As String = "about:blank"
                            Dim found_playlist_id As String = String.Empty

                            If fe.Contains("https://www.youtube.com/playlist?list=") Then
                                Dim title2 As String = fe.IndexOf("""")
                                found_playlist_title = UnicodeJson(fe.Substring(0, title2))
                                found_playlist_title = found_playlist_title.Replace("&lt;?&gt;", String.Empty)
                                found_playlist_title = found_playlist_title.Trim
                                If String.IsNullOrEmpty(found_playlist_title) OrElse found_playlist_title.Length = 0 Then found_playlist_title = "(Playlist N°" & f_counter.ToString & ")"

                                Dim thumb1, thumb2 As Integer
                                thumb1 = fe.IndexOf("""thumbnails"": [{""url"": """)
                                If thumb1 > -1 Then
                                    thumb2 = fe.IndexOf("""", thumb1 + 24)
                                    found_playlist_thumbnail = fe.Substring(thumb1 + 24, thumb2 - thumb1 - 24)
                                End If

                                Dim play1, play2 As Integer
                                play1 = fe.IndexOf("""id"": """)
                                If play1 > -1 Then
                                    play2 = fe.IndexOf("""", play1 + 7)
                                    found_playlist_id = fe.Substring(play1 + 7, play2 - play1 - 7)
                                End If

                                actually_found = True
                                f_counter += 1

                                If f_counter >= vid_offset * disp_vids_per_channel And f_counter < vid_offset * disp_vids_per_channel + disp_vids_per_channel Then
                                    compiledpage.AppendLine(" <TR CLASS=""survol"">")
                                    compiledpage.AppendLine("  <TD VALIGN=TOP WIDTH=160>")
                                    compiledpage.AppendLine("   <A HREF=""/playlist.cgi?id=" & found_playlist_id & """ TARGET=""_new"">")
                                    compiledpage.AppendLine("    <IMG SRC=""playhead.gif"" ALT=""Playlist"" /><BR>")
                                    compiledpage.AppendLine("    <IMG SRC=""getpic.cgi?url=" & Uri.EscapeDataString(found_playlist_thumbnail) & "&amp;type=thumbnail"" ALT=""" & EscapeHtml(found_playlist_title) & """ CLASS=""thumbstyle"" STYLE=""border: 1px solid black; border-radius: 0px; position: relative; top: -2px;"" />")
                                    compiledpage.AppendLine("   </A>")
                                    compiledpage.AppendLine("  </TD>")
                                    compiledpage.AppendLine("  <TD WIDTH=*>")
                                    compiledpage.AppendLine("   <P><B>Playlist #" & f_counter.ToString & ": </B><A HREF=""/playlist.cgi?id=" & found_playlist_id & """ TARGET=""_new"">" & found_playlist_title & "</A></P>")
                                    compiledpage.AppendLine("  </TD>")
                                    compiledpage.AppendLine(" </TR>")
                                    compiledpage.AppendLine()
                                End If
                            End If
                        Next

                        If vid_offset * disp_vids_per_channel > f_counter Then
                            compiledpage.AppendLine("<TR><TD ALIGN=CENTER><H2>Indice en dehors de la plage !</H2></TD></TR>")
                        Else
                            If Not actually_found Then
                                compiledpage.AppendLine("<TR><TD ALIGN=CENTER><H2>Aucune playlist n'a été trouvée !</H2></TD></TR>")
                            End If
                        End If

                        compiledpage.AppendLine("</TABLE></CENTER><BR><BR>")
                        compiledpage.AppendLine()

                        If f_counter > disp_vids_per_channel Then
                            compiledpage.AppendLine("<CENTER><P>")

                            Dim max_page As Integer = CInt(Math.Ceiling(CDbl(f_counter) / CDbl(disp_vids_per_channel)))

                            compiledpage.AppendLine("<FORM METHOD=""GET"" ACTION=""/channel.cgi"">")

                            If vid_offset <> 0 Then
                                compiledpage.Append("<LABEL><A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=" & vid_section & "&amp;page=" & CStr(CInt(vid_offset)) & """>&lt; Page précédente</A></LABEL>&nbsp;|&nbsp;")
                            Else
                                compiledpage.Append("<LABEL>&lt; Page précédente</LABEL>&nbsp;|&nbsp;")
                            End If

                            If vid_offset <> max_page - 1 Then
                                compiledpage.Append("<LABEL><A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=" & vid_section & "&amp;page=" & CStr(CInt(vid_offset + 2)) & """>Page suivante &gt;</A></LABEL>&nbsp;|&nbsp;")
                            Else
                                compiledpage.Append("<LABEL>Page suivante &gt;</LABEL>&nbsp;|&nbsp;")
                            End If

                            compiledpage.AppendLine("<LABEL>Page " & CInt(vid_offset + 1).ToString & " sur " & max_page.ToString & "&nbsp;|&nbsp;Aller à la page: </LABEL>")
                            compiledpage.AppendLine(" <INPUT NAME=""id"" TYPE=""hidden"" VALUE=""" & arg1 & """ />")
                            compiledpage.AppendLine(" <INPUT NAME=""section"" TYPE=""hidden"" VALUE=""" & vid_section & """ />")
                            compiledpage.AppendLine(" <INPUT NAME=""page"" VALUE=""1"" MAXLENGTH=12 SIZE=3 />")
                            compiledpage.AppendLine(" <INPUT TYPE=""submit"" VALUE=""OK"" CLASS=""red_button"" STYLE=""width: 32px;"" />")
                            compiledpage.AppendLine("</FORM>")
                            compiledpage.AppendLine("</P></CENTER><BR><BR>")
                            compiledpage.AppendLine()
                        End If
                    End If
                Else
                    If vid_offset * disp_vids_per_channel > channel_num_vids Then
                        vid_offset = 0 'Si l'offset dépasse le nombre de vidéos
                    End If

                    Dim vc As Integer = 0

                    compiledpage.AppendLine("<CENTER><TABLE BORDER=0 ALIGN=CENTER CELLPADDING=24 CELLSPACING=0>")
                    compiledpage.AppendLine(" <TR>")

                    For i As Integer = vid_offset * disp_vids_per_channel To vid_offset * disp_vids_per_channel + disp_vids_per_channel - 1
                        Dim tmp_prop As New VideoProperties

                        If i < channel_num_vids Then
                            vid_identifiers(i) = vid_identifiers(i).Replace(vbLf, String.Empty)

                            If LooksLikeYoutubeID(vid_identifiers(i)) Then
                                Dim watcharg As String = vid_identifiers(i)

                                SyncLock video_props
                                    If video_props.Count > 1000 Then
                                        Do Until video_props.Count = 1000
                                            video_props.Remove(video_props.Keys(0))
                                        Loop
                                    End If

                                    If Not video_props.ContainsKey(watcharg) Then

                                        Dim op_get_vid As OutputResponse = LaunchProcess("--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<|>%(channel_id)s<|>%(like_count)s<|>%(dislike_count)s"" --no-warnings ""https://www.youtube.com/watch?v=" & watcharg & """")
                                        Dim output3 As String = op_get_vid.OutputData
                                        output3 = output3.Replace(vbLf, String.Empty)
                                        output3 = output3.Replace(vbCr, String.Empty)
                                        If output3.EndsWith("<|>") Then output3 = output3.Remove(output3.Length - 3, 3)
                                        Dim err3 As String = op_get_vid.ErrorData

                                        If Not String.IsNullOrEmpty(output3) Then
                                            Dim output_elements() As String = Nothing

                                            Try
                                                output_elements = output3.Split("<|>")

                                                For j As Integer = 0 To output_elements.Count - 1
                                                    For k As Integer = 0 To &H1F
                                                        output_elements(j) = output_elements(j).Replace(Chr(k), String.Empty)
                                                    Next
                                                Next

                                                output_elements(9) = output_elements(9).Replace(vbCrLf, "<BR>")
                                                output_elements(9) = output_elements(9).Replace(vbCr, "<BR>")
                                                output_elements(9) = output_elements(9).Replace(vbLf, "<BR>")

                                                tmp_prop.ID = output_elements(0)
                                                tmp_prop.Title = CleanText(output_elements(1))
                                                tmp_prop.Views = IIf(LCase(output_elements(2)) = "na", "0", GetThousands(output_elements(2)))
                                                tmp_prop.DateOfRelease = GetDate(output_elements(3))
                                                tmp_prop.Creator = CleanText(output_elements(4))
                                                tmp_prop.Thumbnail = output_elements(5)
                                                tmp_prop.Channel_URL = "/channel.cgi?id=" & CleanText(output_elements(10))
                                                tmp_prop.Like_Count = output_elements(11)
                                                tmp_prop.Dislike_Count = output_elements(12)

                                                If LCase(output_elements(6)) = "na" Then
                                                    tmp_prop.Duration = -1
                                                Else
                                                    tmp_prop.Duration = CInt(output_elements(6))
                                                End If

                                                tmp_prop.Dimensions = IIf(IsNumeric(output_elements(7)), output_elements(7), 640) & ":" & IIf(IsNumeric(output_elements(8)), output_elements(8), 480)
                                                tmp_prop.Description = CleanText(output_elements(9))
                                                tmp_prop.DateAdded = Now

                                                video_props.Add(watcharg, tmp_prop)
                                            Catch ex As Exception

                                            End Try
                                        Else
                                            'Aucune vidéo obtenue, je laisse aller le code, ça va juste afficher une vidéo avec "Titre inconnu".
                                        End If

                                    Else
                                        tmp_prop = video_props(watcharg)
                                    End If
                                End SyncLock

                                vc += 1

                                compiledpage.AppendLine("  <TD WIDTH=160 VALIGN=TOP CLASS=""survol"">")

                                'If vid_section = "shorts" Then
                                '    patternpage &= "   <CENTER><A HREF=""/watch?v=" & tmp_prop.ID & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&type=short"" ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=100 HEIGHT=180 CLASS=""thumbshort"" /></A></CENTER><BR>" & vbCrLf
                                'Else 'Les miniatures des shorts ne sont pas actuellement implémentées. YouTube renvoie des miniatures horizontales, croppées, avec un filigrane (ce qui est déjà opérationnel).
                                'End If

                                If String.IsNullOrEmpty(tmp_prop.ID) OrElse tmp_prop.ID.Length = 0 Then
                                    compiledpage.AppendLine("   <IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" />")
                                    compiledpage.AppendLine("   <P><B>" & tmp_prop.Title & "</B><BR>")
                                    compiledpage.AppendLine("Date:&nbsp;" & tmp_prop.DateOfRelease & "<BR>Vues:&nbsp;" & tmp_prop.Views.Replace(" ", "&nbsp;") & "</P>")
                                    compiledpage.AppendLine("  </TD>")
                                Else
                                    If vid_section = "shorts" Then
                                        compiledpage.AppendLine("   <A HREF=""/short?v=" & tmp_prop.ID & "&amp;list=" & arg1 & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" /></A>")
                                        compiledpage.Append("   <P><A HREF=""/short?v=" & tmp_prop.ID & "&amp;list=" & arg1 & """>" & tmp_prop.Title & "</A>")
                                    Else
                                        compiledpage.AppendLine("   <A HREF=""/watch?v=" & tmp_prop.ID & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" /></A>")
                                        compiledpage.Append("   <P><A HREF=""/watch?v=" & tmp_prop.ID & """>" & tmp_prop.Title & "</A>")
                                    End If

                                    If display_stream_button Then compiledpage.Append(" <A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""playbtn.gif"" BORDER=0 ALT=""Flux direct"" /></A>")
                                    compiledpage.AppendLine()
                                    compiledpage.AppendLine("<BR>")
                                    compiledpage.AppendLine("Date:&nbsp;" & tmp_prop.DateOfRelease & "<BR>Vues:&nbsp;" & tmp_prop.Views.Replace(" ", "&nbsp;") & "</P>")
                                    compiledpage.AppendLine("  </TD>")
                                End If

                                If (vc Mod 3 = 0) Then compiledpage.AppendLine(" </TR>" & vbCrLf & vbCrLf & "  <TR>")
                            Else
                                vc += 1
                                compiledpage.AppendLine("  <TD WIDTH=160 VALIGN=TOP CLASS=""survol"">")
                                compiledpage.AppendLine("   <!-- Data error: VID_ID.Length=" & vid_identifiers.Length.ToString & " -->")
                                compiledpage.AppendLine("  </TD>")
                                If (vc Mod 3 = 0) Then compiledpage.AppendLine(" </TR>" & vbCrLf & vbCrLf & "  <TR>")
                            End If
                        End If
                    Next

                    compiledpage.AppendLine("</TABLE></CENTER><BR><BR>")

                    If channel_num_vids > disp_vids_per_channel Then
                        compiledpage.Append("<CENTER><P>")

                        Dim max_page As Integer = CInt(Math.Ceiling(CDbl(channel_num_vids) / CDbl(disp_vids_per_channel)))

                        compiledpage.AppendLine("<FORM METHOD=""GET"" ACTION=""/channel.cgi"">")

                        If vid_offset <> 0 Then
                            compiledpage.Append("<LABEL><A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=" & vid_section & "&amp;page=" & CStr(CInt(vid_offset)) & """>&lt; Page précédente</A></LABEL>&nbsp;|&nbsp;")
                        Else
                            compiledpage.AppendLine("<LABEL>&lt; Page précédente</LABEL>&nbsp;|&nbsp;")
                        End If

                        If vid_offset <> max_page - 1 Then
                            compiledpage.AppendLine("<LABEL><A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=" & vid_section & "&amp;page=" & CStr(CInt(vid_offset + 2)) & """>Page suivante &gt;</A></LABEL>&nbsp;|&nbsp;")
                        Else
                            compiledpage.AppendLine("<LABEL>Page suivante &gt;</LABEL>&nbsp;|&nbsp;")
                        End If

                        compiledpage.AppendLine("<LABEL>Page " & CInt(vid_offset + 1).ToString & " sur " & max_page.ToString & "&nbsp;|&nbsp;Aller à la page: </LABEL>")
                        compiledpage.AppendLine(" <INPUT NAME=""id"" TYPE=""hidden"" VALUE=""" & arg1 & """ />")
                        compiledpage.AppendLine(" <INPUT NAME=""section"" TYPE=""hidden"" VALUE=""" & vid_section & """ />")
                        compiledpage.AppendLine(" <INPUT NAME=""page"" VALUE=""1"" MAXLENGTH=12 SIZE=3 />")
                        compiledpage.AppendLine(" <INPUT TYPE=""submit"" VALUE=""OK"" CLASS=""red_button"" STYLE=""width: 32px;"" />")
                        compiledpage.AppendLine("</FORM>")

                        compiledpage.AppendLine("</P></CENTER><BR><BR>")
                        compiledpage.AppendLine()
                    End If
                End If

                Select Case vid_section
                    Case "shorts"
                        patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; " & GetThousands(channel_num_vids) & " short(s)</B></P>")
                    Case "playlists"
                        patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; " & GetThousands(f_counter) & " playlist(s)</B></P>")
                    Case "streams"
                        patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; " & GetThousands(channel_num_vids) & " vidéo(s) en live</B></P>")
                    Case Else
                        patternpage.AppendLine("   <P><B>@" & channel_upid & " &bull; " & GetThousands(channel_followers) & " abonné(s) &bull; " & GetThousands(channel_num_vids) & " vidéo(s)</B></P>")
                End Select

                patternpage.AppendLine("   <P>" & UnicodeJson(channel_desc) & "</P><BR>")
                patternpage.AppendLine("  </TD>")
                patternpage.AppendLine(" </TR>")
                patternpage.AppendLine(" <TR>")
                patternpage.Append("  <TD COLSPAN=3><CENTER><H2 CLASS=""black_label""><B>Sections de la chaîne :</B></H1>")

                Select Case vid_section
                    Case "videos"
                        patternpage.AppendLine("<B>VID&Eacute;OS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                    Case "shorts"
                        patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <B>SHORTS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                    Case "playlists"
                        patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <B>PLAYLISTS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                    Case "streams"
                        patternpage.AppendLine("<A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=videos"">VID&Eacute;OS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <B>LIVES</B></CENTER></TD>")
                    Case Else
                        patternpage.AppendLine("<B>VID&Eacute;OS</B> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=shorts"">SHORTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=playlists"">PLAYLISTS</A> - <A HREF=""/channel.cgi?id=" & arg1 & "&amp;section=streams"">LIVES</A></CENTER></TD>")
                End Select

                patternpage.AppendLine(" </TR>")
                patternpage.AppendLine("</TABLE></CENTER><BR>")

                'Ajouter ce qui a été trouvé auparavant
                patternpage.AppendLine(compiledpage.ToString)

                patternpage.AppendLine(footer)

                Dim index_resp As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                                           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                           "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                                           "Cache-Control: no-cache, no-store, must-revalidate" & vbCrLf &
                                           "Pragma: no-cache" & vbCrLf &
                                           "Expires: 0" & vbCrLf &
                                           "Connection: close" & vbCrLf &
                                           "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                number_of_channels -= 1
                client.Close()
            End If
        ElseIf request.StartsWith("GET /playlist.cgi?id=") Then
            'Afficher une playlist *****************************************************************************************************************************************************
            Dim arg1 As String = Nothing
            Dim vid_offset As Integer = 0
            Dim error_encountered As Boolean = False

            Try
                arg1 = request.Split(" ")(1)
                arg1 = arg1.Remove(0, 17)
            Catch ex As Exception
                error_encountered = True
            End Try

            If error_encountered OrElse String.IsNullOrEmpty(arg1) OrElse arg1.Length = 0 Then
                WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

                Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Impossible d'afficher la liste de lecture demandée, car aucun identifiant de playlist n'a été spécifié.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                WriteLog("Une playlist a été demandée, mais aucun identifiant n'a été communiqué.", ConsoleColor.Red, client)
                client.Close()
            Else
                Dim url_params() As String

                If arg1.EndsWith("&") Then
                    arg1 = arg1.Remove(arg1.Length - 1, 1)
                End If

                If arg1.Contains("&") Then
                    Dim tmp_arg As String = arg1
                    arg1 = arg1.Substring(0, arg1.IndexOf("&")) 'Prendre l'identifiant de la chaîne elle-même
                    tmp_arg = tmp_arg.Remove(0, tmp_arg.IndexOf("&") + 1)

                    If tmp_arg.Contains("=") Then
                        url_params = tmp_arg.Split("&")

                        For Each u As String In url_params
                            Dim sub_params() As String = u.Split("=")
                            Select Case sub_params(0)
                                Case "page"
                                    If IsNumeric(sub_params(1)) Then
                                        Try
                                            vid_offset = CInt(sub_params(1)) - 1
                                        Catch ex As Exception
                                            vid_offset = 0
                                        End Try
                                    End If
                            End Select
                        Next
                    End If
                End If

                If vid_offset < 0 Then vid_offset = 0 'Ramener à zéro

                WriteLog("Consultation de la playlist " & arg1 & IIf(vid_offset = 0, String.Empty, ", page " & CStr(vid_offset + 1)) & "...", ConsoleColor.Blue, client)

                Dim op_playlist As OutputResponse = LaunchProcess("-J --playlist-items 1 ""https://www.youtube.com/playlist?list=" & arg1 & """")
                Dim output4 As String = op_playlist.OutputData
                Dim err4 As String = op_playlist.ErrorData

                Dim playlist_name As String = "&lt;Nom de playlist inconnu&gt;"
                Dim playlist_desc As String = "&lt;Aucune description disponible&gt;"
                Dim playlist_thumbnail As String = "about:blank"
                Dim channel_title As String = "&lt;Nom de chaîne d'origine inconnu&gt;"
                Dim channel_url As String = "about:blank"

                If String.IsNullOrEmpty(output4) OrElse output4.StartsWith("null") Then
                    Dim baddata As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La playlist demandée n'existe pas selon les serveurs YouTube, ou ne peut être traitée pour l'instant.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                    Try
                        stream.Write(baddata, 0, baddata.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        client.Close()
                        Exit Sub
                    End Try

                    client.Close()
                    Exit Sub
                End If

                Dim param1, param2 As Integer
                Dim playlist_num_vids As Integer = 0

                output4 = output4.Replace("\""", "&quot;")

                param1 = output4.IndexOf("""channel"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 12)
                    channel_title = output4.Substring(param1 + 12, param2 - param1 - 12)
                End If

                param1 = output4.IndexOf("""description"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 16)
                    playlist_desc = output4.Substring(param1 + 16, param2 - param1 - 16)
                    playlist_desc = playlist_desc.Replace("&quot;", """")
                    playlist_desc = EscapeHtml(playlist_desc)
                    If String.IsNullOrEmpty(playlist_desc) OrElse playlist_desc.Length = 0 Then playlist_desc = "Aucune description disponible."
                End If

                param1 = output4.IndexOf("""title"": ")
                If param1 > -1 Then
                    param2 = output4.IndexOf(",", param1 + 9)
                    playlist_name = output4.Substring(param1 + 9, param2 - param1 - 9)
                    playlist_name = playlist_name.Replace("""", String.Empty)
                End If

                'Gestion des playlists gardées en mémoire
                If playlist_list.Count > 1000 Then
                    Do Until playlist_list.Count = 1000
                        playlist_list.Remove(playlist_list.Keys(0))
                    Loop
                End If

                If Not playlist_list.ContainsKey(arg1) Then
                    playlist_list.Add(arg1, playlist_name)
                End If

                param1 = output4.IndexOf("""thumbnails"": [{""url"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 24)
                    playlist_thumbnail = output4.Substring(param1 + 24, param2 - param1 - 24)
                End If

                param1 = output4.IndexOf("""channel_id"": """)
                If param1 > -1 Then
                    param2 = output4.IndexOf("""", param1 + 15)
                    channel_url = output4.Substring(param1 + 15, param2 - param1 - 15)
                End If

                'Liste des vidéos de la playlist
                Dim op_playlist_2 As OutputResponse = LaunchProcess("--flat-playlist --print ""%(id)s<|>"" ""https://www.youtube.com/playlist?list=" & arg1 & """")
                Dim output5 As String = op_playlist_2.OutputData
                output5 = output5.Replace(vbLf, String.Empty)
                output5 = output5.Replace(vbCr, String.Empty)
                If output5.EndsWith("<|>") Then output5 = output5.Remove(output5.Length - 3, 3)
                Dim err5 As String = op_playlist_2.ErrorData

                If String.IsNullOrEmpty(output5) OrElse output5.Length = 0 OrElse output5.StartsWith("null") Then
                    Dim baddata As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La playlist demandée n'existe pas selon les serveurs YouTube, ou ne contient pas d'informations traitables.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                    Try
                        stream.Write(baddata, 0, baddata.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        client.Close()
                        Exit Sub
                    End Try

                    client.Close()
                    Exit Sub
                End If

                Dim vid_identifiers() As String = output5.Split("<|>")
                playlist_num_vids = vid_identifiers.Count
                patternpage.AppendLine(InitValues("Liste de lecture '" & playlist_name & "'", , wanted_skin, , used_player))

                patternpage.AppendLine(" <CENTER><H1 CLASS=""black_label"">Liste de lecture</H1></CENTER>")
                patternpage.AppendLine("<BR><CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=600 CELLPADDING=16 CELLSPACING=0>")
                patternpage.AppendLine(" <TR CLASS=""survol"">")
                patternpage.AppendLine("  <TD WIDTH=92 VALIGN=TOP><BR>")
                patternpage.AppendLine("   <IMG SRC=""/playhead.gif"" ALT=""Liste de lecture"" /><BR>")
                patternpage.AppendLine("   <IMG SRC=""/getpic.cgi?url=" & playlist_thumbnail & "&amp;type=thumbnail"" ALT=""Playlist de " & channel_title & """ WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" STYLE=""border: 1px solid black; border-radius: 0px; position: relative; top: -2px;"" />")
                patternpage.AppendLine("  </TD>")
                patternpage.AppendLine("  <TD WIDTH=* VALIGN=TOP>")
                patternpage.AppendLine("   <P><BR><B>Playlist '" & UnicodeJson(playlist_name) & "' de <A HREF=""/channel.cgi?id=" & channel_url & """>" & UnicodeJson(channel_title) & "</A> (" & CStr(playlist_num_vids) & " vidéo" & IIf(playlist_num_vids > 1, "s", String.Empty) & ")</B></P>")
                patternpage.AppendLine("   <P><B>Description:</B> " & UnicodeJson(playlist_desc) & "</P>")
                patternpage.AppendLine("  </TD>")
                patternpage.AppendLine(" </TR>")
                patternpage.AppendLine("</TABLE></CENTER><BR>")

                'If vid_offset * disp_vids_per_channel > Math.Ceiling(playlist_num_vids / disp_vids_per_channel) Then
                '    vid_offset = 0 'Si l'offset dépasse le nombre de vidéos
                'End If

                Dim vc As Integer = 0
                Dim found_videos As Integer = 0

                patternpage.AppendLine("<CENTER><TABLE BORDER=0 ALIGN=CENTER CELLPADDING=24 CELLSPACING=0>")
                patternpage.AppendLine(" <TR>")

                For i As Integer = vid_offset * disp_vids_per_channel To vid_offset * disp_vids_per_channel + disp_vids_per_channel - 1
                    Dim tmp_prop As VideoProperties

                    If i < playlist_num_vids Then
                        vid_identifiers(i) = vid_identifiers(i).Replace(vbLf, String.Empty)

                        If LooksLikeYoutubeID(vid_identifiers(i)) Then
                            Dim watcharg As String = vid_identifiers(i)
                            found_videos += 1

                            tmp_prop = GetVideo(watcharg)

                            vc += 1

                            patternpage.AppendLine("  <TD WIDTH=160 VALIGN=TOP CLASS=""survol"">")

                            If String.IsNullOrEmpty(tmp_prop.ID) Then
                                patternpage.AppendLine("   <IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" />")
                                patternpage.Append("   <P><B>" & tmp_prop.Title & "</B>")
                            Else
                                patternpage.AppendLine("   <A HREF=""/watch?v=" & tmp_prop.ID & "&amp;list=" & arg1 & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" /></A>")
                                patternpage.Append("   <P><A HREF=""/watch?v=" & tmp_prop.ID & "&amp;list=" & arg1 & """>" & tmp_prop.Title & "</A>")
                            End If

                            If display_stream_button Then patternpage.AppendLine(" <A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""playbtn.gif"" BORDER=0 ALT=""Flux direct"" /></A>")

                            patternpage.AppendLine()
                            patternpage.AppendLine("<BR>Date:&nbsp;" & tmp_prop.DateOfRelease & "<BR>Vues:&nbsp;" & tmp_prop.Views.Replace(" ", "&nbsp;") & "</P>")
                            patternpage.AppendLine("  </TD>")
                            If (vc Mod 3 = 0) Then patternpage.AppendLine(" </TR>" & vbCrLf & vbCrLf & "  <TR>")
                        Else
                            vc += 1
                            patternpage.AppendLine("  <TD WIDTH=160 VALIGN=TOP>")
                            patternpage.AppendLine("   <!-- Data error: VID_ID.Length=" & vid_identifiers.Length.ToString & " -->")
                            patternpage.AppendLine("  </TD>")
                            If (vc Mod 3 = 0) Then patternpage.AppendLine(" </TR>" & vbCrLf & vbCrLf & "  <TR>")
                        End If
                    End If
                Next

                If found_videos = 0 Then
                    patternpage.AppendLine("<TR><TD><CENTER><P ALIGN=CENTER><H2>Aucune vidéo trouvée dans cette liste, ou numéro de page invalide.</H2></P></CENTER><DIV CLASS=bodysep></DIV></TD></TR>")
                End If

                patternpage.AppendLine("</TABLE></CENTER><BR><BR>")

                If playlist_num_vids > disp_vids_per_channel And found_videos > 0 Then
                    patternpage.AppendLine("<CENTER><P>")

                    Dim max_page As Integer = CInt(Math.Ceiling(CDbl(playlist_num_vids) / CDbl(disp_vids_per_channel)))

                    patternpage.AppendLine("<FORM METHOD=""GET"" ACTION=""/playlist.cgi"">")

                    If vid_offset <> 0 Then
                        patternpage.Append("<LABEL><A HREF=""/playlist.cgi?id=" & arg1 & "&amp;page=" & CStr(CInt(vid_offset)) & """>&lt; Page précédente</A></LABEL>&nbsp;|&nbsp;")
                    Else
                        patternpage.Append("<LABEL>&lt; Page précédente</LABEL>&nbsp;|&nbsp;")
                    End If

                    If vid_offset <> max_page - 1 Then
                        patternpage.Append("<LABEL><A HREF=""/playlist.cgi?id=" & arg1 & "&amp;page=" & CStr(CInt(vid_offset + 2)) & """>Page suivante &gt;</A></LABEL>&nbsp;|&nbsp;")
                    Else
                        patternpage.Append("<LABEL>Page suivante &gt;</LABEL>&nbsp;|&nbsp;")
                    End If

                    patternpage.AppendLine("<LABEL>Page " & CInt(vid_offset + 1).ToString & " sur " & max_page.ToString & "&nbsp;|&nbsp;Aller à la page: </LABEL>")
                    patternpage.AppendLine(" <INPUT NAME=""id"" TYPE=""hidden"" VALUE=""" & arg1 & """ />")
                    patternpage.AppendLine(" <INPUT NAME=""page"" VALUE=""1"" MAXLENGTH=12 SIZE=3 />")
                    patternpage.AppendLine(" <INPUT TYPE=""submit"" VALUE=""OK"" CLASS=""red_button"" STYLE=""width: 32px;"" />")
                    patternpage.AppendLine("</FORM>")

                    patternpage.AppendLine("</P></CENTER><BR><BR>")
                    patternpage.AppendLine()
                End If

                patternpage.AppendLine(footer)

                Dim index_resp As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                                           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                           "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                                           "Connection: close" & vbCrLf &
                                           "Cache-Control: no-cache, no-store, must-revalidate" & vbCrLf &
                                           "Pragma: no-cache" & vbCrLf &
                                           "Expires: 0" & vbCrLf &
                                           "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try
                client.Close()
            End If
        ElseIf request.StartsWith("GET /playlist.cgi") Then
            'Les requêtes directes sur playlist.cgi entraînent une erreur 400 (requête invalide).
            WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Vous devez spécifier l'identifiant de playlist que vous voulez consulter (avec <I>id=PLxxxxxxxxxxx</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                client.Close()
                Exit Sub
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /channel.cgi") Then
            'Les requêtes directes sur channel.cgi entraînent une erreur 400 (requête invalide).
            WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Vous devez spécifier la chaîne sur laquelle vous voulez naviguer (avec <I>id=UCxxxxxxxxxxxx</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                client.Close()
                Exit Sub
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /cache.cgi") Then
            patternpage.AppendLine(InitValues("Vidéos en cache", , wanted_skin, , used_player, , True))

            Dim allowed_ip As Boolean = False
            Dim arg1 As String = Split(request)(1)
            Dim current_offset As Integer = 0

            For a As Integer = 16 To 31 'Classe B de réseaux locaux
                If GetClientIP(client).StartsWith("172." & a.ToString & ".") Then
                    allowed_ip = True
                End If
            Next

            If GetClientIP(client) = "127.0.0.1" Or GetClientIP(client).StartsWith("192.168.1") Or GetClientIP(client).StartsWith("169.254") Or GetClientIP(client).StartsWith("10.") Or allowed_ip Then 'IP en localhost, LAN en classe A et C, et en APIPA
                If arg1.Contains("?offset=") Then
                    arg1 = arg1.Remove(0, arg1.IndexOf("?offset=") + 8).Trim
                    Try
                        current_offset = CInt(arg1)
                    Catch ex As Exception
                        current_offset = 0
                    End Try
                End If

                If current_offset < 0 Then
                    current_offset = 0
                End If

                If current_offset Mod 50 <> 0 Then 'Si l'utilisateur trafique l'offset pour naviguer sur 54 par exemple, ça va ramener à 50.
                    current_offset -= current_offset Mod 50
                End If

                If current_offset >= video_props.Count Then
                    current_offset = 0
                End If

                patternpage.AppendLine("<H2 CLASS=""black_label"" STYLE=""width: 800px;"">Cache mémoire des vidéos</H2><BR>")

                If video_props.Count = 0 Then
                    patternpage.AppendLine("<CENTER><P><B><FONT SIZE=3>Il n'y a actuellement aucune vidéo dans le cache mémoire.<BR>Veuillez naviguer sur RetroYT pour enrichir le cache vidéo du proxy.</FONT></B></P></CENTER><BR>")
                    patternpage.AppendLine("<DIV CLASS=bodysep>&nbsp;</DIV>")
                    WriteLog("Cache des vidéos demandé. Il n'y a actuellement aucune vidéo en cache.", ConsoleColor.Blue, client)
                Else
                    patternpage.AppendLine("<CENTER><P><B><FONT SIZE=3>Il y a actuellement " & video_props.Count.ToString & " vidéo(s) dans le cache mémoire. Voici le tableau :</FONT></B></P></CENTER><BR>")
                    patternpage.AppendLine("<BR><CENTER><TABLE BORDER=1 WIDTH=800 ALIGN=CENTER CELLPADDING=4 CELLSPACING=0>")
                    patternpage.AppendLine(" <TR>")
                    patternpage.AppendLine("  <TD BGCOLOR=BLACK ALIGN=CENTER><B><FONT COLOR=WHITE>Identifiant vidéo</FONT></B></TD>")
                    patternpage.AppendLine("  <TD BGCOLOR=BLACK ALIGN=CENTER><B><FONT COLOR=WHITE>Titre de la vidéo</FONT></B></TD>")
                    patternpage.AppendLine("  <TD BGCOLOR=BLACK ALIGN=CENTER><B><FONT COLOR=WHITE>Date d'ajout</FONT></B></TD>")
                    patternpage.AppendLine(" </TR>")

                    Dim j As String() = video_props.Keys.ToArray
                    Dim max_page As Integer = Math.Ceiling(video_props.Count / 50)

                    If video_props.Count < 50 Then
                        For i As Integer = 0 To j.Count - 1
                            Dim found_video As VideoProperties = video_props(j(i))
                            Dim t_title As String = found_video.Title
                            If t_title.Length > 256 Then t_title = t_title.Substring(0, 256) & "..."
                            patternpage.AppendLine(" <TR>")
                            patternpage.AppendLine("  <TD><A HREF=""/watch?v=" & found_video.ID & """>" & found_video.ID & "</A></TD><TD>" & t_title & "</TD><TD>" & found_video.DateAdded & "</TD>")
                            patternpage.AppendLine(" </TR>")
                        Next

                        patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
                        patternpage.AppendLine("<DIV CLASS=bodysep>&nbsp;</DIV>")
                    Else
                        For i As Integer = current_offset To Math.Min(video_props.Count, current_offset + 50) - 1 '0 To j.Count - 1
                            Dim found_video As VideoProperties = video_props(j(i))
                            Dim t_title As String = found_video.Title
                            If t_title.Length > 256 Then t_title = t_title.Substring(0, 256) & "..."
                            patternpage.AppendLine(" <TR>")
                            patternpage.AppendLine("  <TD><A HREF=""/watch?v=" & found_video.ID & """>" & found_video.ID & "</A></TD><TD>" & t_title & "</TD><TD>" & found_video.DateAdded & "</TD>")
                            patternpage.AppendLine(" </TR>")
                        Next

                        patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
                        patternpage.AppendLine()

                        patternpage.Append("<P ALIGN=CENTER>Page ")
                        For i As Integer = 0 To max_page - 1
                            If i * 50 <> current_offset Then patternpage.Append("<A HREF=""/cache.cgi?offset=" & CStr(i * 50) & """>")
                            patternpage.Append(CStr(i + 1))
                            If i * 50 <> current_offset Then patternpage.Append("</A>")
                            patternpage.Append(" - ")
                        Next

                        patternpage = patternpage.Remove(patternpage.Length - 3, 3)
                        patternpage.AppendLine("</P><BR><BR>")
                        patternpage.AppendLine()
                    End If

                    WriteLog("Cache des vidéos demandé. Il y a " & video_props.Count.ToString & " vidéo(s) en cache." & IIf(current_offset > 0, " Lecture à l'offset " & current_offset.ToString & ".", String.Empty), ConsoleColor.Blue, client)
                End If

                patternpage.AppendLine(footer)

                Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            Else
                'Les autres requêtes entraînent une erreur 400 (requête invalide).
                WriteLog("Erreur HTTP #401: Non autorisé.", , client)

                Dim baddata As Byte() = GetHTTPBytes(401, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 401 - Accès non autorisé</H1>" & vbCrLf & "<P>Vous n'avez pas accès à cette page, seul l'administrateur ou un utilisateur du réseau local peut y accéder.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /about.htm") Then
            'Afficher le "à propos" du proxy
            patternpage.AppendLine(InitValues("À propos de RetroYT", , wanted_skin, , used_player, , True))
            WriteLog("Page des informations sur le logiciel envoyée.", , client)

            patternpage.AppendLine("<BR><CENTER><H1 CLASS=""black_label"" STYLE=""width: 780px;"">Documentation de RetroYT</H1></CENTER>")
            patternpage.AppendLine("<BR><BR><CENTER><DIV STYLE=""display: block; width: 780px; margin-left: auto; margin-right: auto; text-align: left; text-align: justify;""><B>RetroYT</B> est un proxy multimédia pour YouTube développé en Visual Basic .NET 2022 par Monokeros. La version actuelle, la Bêta 9.0, a été publiée le 2 juillet 2026. Ce projet est distribué gratuitement (sous la licence «&nbsp;freeware&nbsp;»), sans aucune garantie explicite ou implicite. L'auteur ne pourra être tenu responsable d'éventuels dommages matériels, logiciels, des éventuelles pertes de données, ou dysfonctionnements résultant de son utilisation, y compris dans un cadre normal.<BR>")
            patternpage.AppendLine("Le projet vise principalement à restaurer la compatibilité de YouTube avec des systèmes d'exploitation, navigateurs web et lecteurs multimédia anciens ou obsolètes, à travers le relais de connexions, formatage vers un code HTML, et l'intégration de formats vidéo historiques, lisible par les navigateurs de toute époque. Aussi, le temps de chargement entre les pages peut être assez long. Puisque RetroYT fait usage de yt-dlp, les temps d'attente sont allongés pour les recherches YouTube effectuées avec des clients non-officiels. De plus, l'obtention du contenu et sa conversion peuvent également prendre du temps.")
            patternpage.AppendLine("<BR><BR><BR>")

            patternpage.AppendLine("<DIV STYLE=""display: block; border-radius: 8px; margin-left: 225px; border: 1px solid " & IIf(wanted_skin = "dark", "white", "black") & "; padding: 8px 8px 8px 8px; width: 40%;""><BR><CENTER><BIG><BIG><B>Sommaire: </B></BIG></BIG></CENTER><BR>")
            patternpage.AppendLine("<A HREF=""#introduction"">I. Introduction</A><BR>")
            patternpage.AppendLine("<A HREF=""#parameters"">II. Fonctionnalités</A><BR>")
            patternpage.AppendLine("<A HREF=""#precautions"">III. Précautions</A><BR>")
            patternpage.AppendLine("<A HREF=""#configuration"">IV. Configuration</A><BR>")
            patternpage.AppendLine("<A HREF=""#useget"">V. Utilisation du paramètre GET</A><BR>")
            patternpage.AppendLine("<A HREF=""#playlists"">VI. Listes de lecture (Playlists)</A><BR>")
            patternpage.AppendLine("<A HREF=""#credits"">VII. Remerciements</A><BR>&nbsp;</DIV><BR><BR>")
            patternpage.AppendLine()

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<CENTER><H2><A NAME=""introduction"" STYLE=""color: red;"">I. Introduction</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<CENTER><H2><A NAME=""introduction"" STYLE=""color: black;"">I. Introduction</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("Le nom «&nbsp;RetroYT&nbsp;» provient du terme «&nbsp;rétro&nbsp;», désignant de manière générale quelque chose d'ancien, de classique ou «&nbsp;à l'ancienne&nbsp;». Le logiciel repose sur un serveur Web codé directement dans l'application (dit «&nbsp;hardcodé&nbsp;»), servant d'intermédiaire entre YouTube et le navigateur client utilisé par l'utilisateur. L'objectif principal du projet est de restaurer un accès fonctionnel à YouTube sur des navigateurs et systèmes d'exploitation devenus trop anciens pour prendre en charge la version moderne du site. Bien que RetroYT puisse également être utilisé depuis un navigateur récent comme un proxy classique, pour fournir une version allégée du site, ce n'est pas sa vocation première. De nombreux proxies YouTube modernes existent déjà et offrent généralement de meilleures performances et une compatibilité plus étendue avec les standards Web actuels.<BR><B>RetroYT</B> vise avant tout à permettre la recherche et la lecture de vidéos YouTube depuis des environnements anciens ou désuets, tels que Windows 3.11, Windows 95, Windows 98, Windows NT 4.0, Windows 2000, certaines anciennes versions de MacOS, ainsi que divers systèmes UNIX/Linux historiques. La solution a également été testée sous Windows XP et Windows 11 avec succès. &Eacute;tant donné l'identité rétrocompatible de ce projet, il est donc parfaitement normal de retrouver, au sein de ce projet, du code HTML volontairement ancien, des méthodes d'intégration multimédia historiques, ou encore l'utilisation de technologies aujourd'hui abandonnées comme ActiveX, RealPlayer, des anciennes versions de QuickTime, Flash Player, ou les plugins NPAPI. L'ensemble du projet cherche à reproduire, autant que possible, une expérience cohérente avec les capacités techniques du Web des années 1990 et du début des années 2000, tout en offrant une expérience de navigation proche des services Internet actuels. Pour cela, la prise en charge des émojis a également été ajoutée dans la Bêta 8.0.<BR><BR>")

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""parameters"" STYLE=""color: red;"">II. Fonctionnalités</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""parameters"" STYLE=""color: black;"">II. Fonctionnalités</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("<B>RetroYT</B> propose un ensemble de paramètres permettant d'adapter le fonctionnement du proxy aux capacités matérielles et logicielles du système cible. La méthode de récupération des fichiers depuis YouTube est agnostique. En d'autres termes, le proxy prendra le premier format venu, il peut être en extension MP4, WebM, ou MKV, etc. avec un encodage comme VP8, VP9, AV1, AV2, H.264, etc. Pour le fichier de destination, un second cache existe et permet de convertir dans un autre format compatible avec les anciennes configurations. Ces formats sont entièrement configurables depuis le navigateur client. L'utilisateur peut notamment choisir la taille du lecteur vidéo, le format et les codecs employés pour la conversion, ainsi que le nombre d'images par seconde. Pour les systèmes les plus anciens, comme Windows 95 ou Windows NT 4.0, l'utilisation des codecs MSVideo1 (Microsoft Video 1), MPEG-1 ou WMV1 est fortement recommandée, en raison de leur excellente compatibilité avec les anciennes versions de Windows. Comme beaucoup de codecs historiques, celui-ci produit toutefois des fichiers assez volumineux, en particulier pour les vidéos dépassant plusieurs minutes. Le codec Cinepak est aussi très rétrocompatible, mais peut mettre beaucoup de temps à être encodé. Pour les systèmes Apple, le format MOV avec les codecs RPZA ou Sorenson sont vivement conseillés. Les systèmes Linux étant très compatibles avec les formats PC, je conseille le format AVI MPEG-4 pour la lecture, et le format MP4 pour les systèmes plus récents, y compris ceux de Microsoft et Apple.<BR>Selon la puissance de votre machine, votre quantité de mémoire disponible ou la vitesse de votre connexion réseau, le transfert et la lecture des vidéos peuvent devenir plus difficiles. La résolution vidéo peut être choisie parmi un certain nombre de valeurs prédéfinies (96p, 120p, 144p, 240p, 360p, 480p, 720p et 1080p), ou laissée en mode automatique afin que le serveur sélectionne lui-même le format le plus approprié. Certains codecs anciens possèdent volontairement des limitations de résolution ou de format d'image, principalement pour des raisons de compatibilité avec les anciens lecteurs multimédia ou les contraintes matérielles des systèmes ciblés. Pareil pour le nombre d'images. Sélectionner le mode 60 images par seconde sur un format tel AVI Cinepak va immédiatement ramener à 30 images par seconde. Les vidéos de plus de 3 heures ne sont pas disponibles à la lecture. Celles de 1 heure peuvent être ignorées dans les paramètres du client.<BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("Le mode d'intégration du lecteur vidéo est également configurable. RetroYT peut utiliser différentes méthodes historiques de lecture multimédia, parmi lesquelles&nbsp;:")
            patternpage.AppendLine()

            patternpage.AppendLine("<UL>")
            patternpage.AppendLine(" <LI>L'intégration ActiveX de Windows Media Player 6.4 ou supérieur ;</LI>")
            patternpage.AppendLine(" <LI>La balise HTML &lt;embed&gt; ;</LI>")
            patternpage.AppendLine(" <LI>L'intégration de lecteurs externes tels que VLC, QuickTime ou RealPlayer ;</LI>")
            patternpage.AppendLine(" <LI>Le fameux lecteur Flash Player, très utilisé aux grands débuts de YouTube ;</LI>")
            patternpage.AppendLine(" <LI>Ou encore la balise &lt;video&gt;, sous navigateurs modernes compatibles HTML5 (sortis après 2008).</LI>")
            patternpage.AppendLine("</UL>")

            patternpage.AppendLine("L'apparence générale de l'interface Web peut également être personnalisée grâce à plusieurs thèmes graphiques&nbsp;:")
            patternpage.AppendLine()

            patternpage.AppendLine("<UL>")
            patternpage.AppendLine(" <LI><B>Classic :</B> Interface inspirée du site de YouTube des années 2000 ;</LI>")
            patternpage.AppendLine(" <LI><B>Cosmic :</B> Reproduction fidèle du thème «&nbsp;Cosmic Panda&nbsp;» utilisé officiellement entre 2011 et 2013 sur ce même site ;</LI>")
            patternpage.AppendLine(" <LI><B>Modern :</B> Interface proche du YouTube actuel ;</LI>")
            patternpage.AppendLine(" <LI><B>Dark Mode :</B> Affichage clair sur fond sombre ;</LI>")
            patternpage.AppendLine(" <LI><B>Rose :</B> Thème aux couleurs violacées, rappelant certaines interfaces Web des années 1990 ;</LI>")
            patternpage.AppendLine(" <LI><B>Aqua :</B> Thème aux couleurs bleues, rappelant l'eau ;</LI>")
            patternpage.AppendLine(" <LI><B>Mint :</B> Thème aux couleurs vertes, rappelant la nature ;</LI>")
            patternpage.AppendLine(" <LI><B>Sunshine :</B> Thème aux couleurs dorées, rappelant le Soleil ;</LI>")
            patternpage.AppendLine(" <LI><B>Monochrome :</B> Thème aux couleurs monochromes, pour ceux qui ont des difficultés visuelles, ou qui préfèrent les interfaces sobres.</LI>")
            patternpage.AppendLine("</UL>")
            patternpage.AppendLine()

            patternpage.AppendLine("Ces options permettent d'adapter RetroYT aussi bien à des machines très anciennes qu'à des systèmes plus récents, tout en conservant une esthétique cohérente avec les différentes époques du Web. Il est intéressant de noter qu'on peut lire aussi des flux vidéo depuis un lecteur externe, sans passer par l'interface Web. Il suffit pour cela de naviguer sur <I>http://adresse_serveur/stream?v=id_video</I> pour lire directement dans un lecteur externe comme VLC. Vous pouvez aussi chercher et lire la première vidéo trouvée de façon immédiate en naviguant sur <I>http://adresse_serveur/lucky?q=motclef</I>. Par défaut, le format permutera automatiquement sur MP4 si vous utilisez VLC. Notez bien que vous pouvez utiliser les paramètres GET documentés dans la <A HREF=""about.htm#useget"">partie V</A> de cette documentation.<BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("Il est également possible de naviguer dans les chaînes YouTube, et d'explorer leur contenu. Les URLs pointant vers les chaînes ressemblent à http://serveur/channel.cgi?id=UCxxxx où UCxxxx est l'identifiant unique de la chaîne. Vous pouvez consulter les vidéos qui sont affichées sous forme de pages, par groupe de 9, 18 ou 27 selon les paramètres du client. L'affichage est paramétré sur 18 vidéos par défaut, mais je conseille de mettre sur 9 vidéos pour les très anciennes configurations. L'affichage de 27 vidéos peut prendre un certain temps à être chargé. Il y a aussi un volet de suggestions à droite de la vidéo en cours de lecture, et une liste de commentaires en dessous, comme sous le vrai YouTube. Vous pouvez consulter tous les commentaires en cliquant sur le lien intitulé""Afficher tous les commentaires"" sous la section éponyme. Le lien pointe vers une URI ressemblant à http://serveur/com.cgi?v=xxxxxxxxxxx, qui sera ouverte dans un nouvel onglet/fenêtre. Vous pourrez également naviguer entre les pages de commentaires, qui sont affichés par groupes de 10, 20, 50 ou 100 commentaires.<BR><BR>")

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""precautions"" STYLE=""color: red;"">III. Précautions</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""precautions"" STYLE=""color: black;"">III. Précautions</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("<B>RetroYT</B> est distribué sous licence freeware/open source et ne doit pas être revendu sans l'autorisation explicite de son auteur. Afin de conserver une compatibilité maximale avec les anciens navigateurs Web et systèmes d'exploitation, le proxy ne met volontairement pas en œuvre certaines technologies modernes de sécurisation des communications, notamment SSL/TLS côté client. Les échanges entre RetroYT et YouTube utilisent bien des connexions sécurisées modernes, mais les communications entre le client et le proxy restent, quant à elles, entièrement non chiffrées. En effet, nombre d'anciens navigateurs ne prennent pas en charge SSL/TLS, surtout dans leurs dernières versions. Le HTTP sans chiffrement est une solution universelle pour se connecter au serveur.<BR>Pour cette raison, RetroYT est principalement destiné à une utilisation au sein d'un réseau local (LAN), sur une machine personnelle ou dans un environnement contrôlé. Il est fortement déconseillé d'exposer directement le proxy sur Internet ou de l'utiliser sur un réseau public non sécurisé, sauf si vous utilisez des solutions complémentaires de protection telles qu'un VPN ou un tunnel sécurisé.<BR><BR>")
            patternpage.AppendLine("RetroYT utilise également un système de cache local afin d'améliorer les performances et limiter les téléchargements répétés. Six dossiers principaux sont utilisés&nbsp;:")
            patternpage.AppendLine()

            patternpage.AppendLine("<UL>")
            patternpage.AppendLine(" <LI>Le dossier <I>thumbs</I> : Cache des miniatures des vidéos, avatars, bannières, etc. ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>srccache</I> : Stockage des vidéos sources et mises en cache pour être converties ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>vidcache</I> : Stockage des vidéos converties et mises en cache pour être envoyées au client ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>prclocks</I> : Stockage de fichiers LOCK, qui permettent de mémoriser les téléchargements ou conversions en cours d'exécution, et de les stopper ainsi que supprimer les fichiers temporaires, en cas de redémarrage après un plantage ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>comments</I> : Stockage des commentaires YouTube trouvés pour pouvoir les lire (si toutefois l'option est activée).</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>tmp_pic</I> : Cache pour stocker les images avant redimensionnement pour les vieux navigateurs.</LI>")
            patternpage.AppendLine("</UL>")
            patternpage.AppendLine()

            patternpage.AppendLine("Ces dossiers peuvent être vidés manuellement si l'espace disque disponible devient insuffisant. Normalement, le logiciel gère lui-même la taille du cache et/ou le nombre de fichiers. Le dossier <I>srvlogs</I> contient tous les fichiers de rapport de connexion et des actions du serveur, avec heure et date. Bien que ces fichiers soient facultatifs et aisément supprimables, en revanche, certains fichiers et répertoires sont indispensables au fonctionnement du logiciel et ne doivent pas être supprimés&nbsp;:")
            patternpage.AppendLine()

            patternpage.AppendLine("<UL>")
            patternpage.AppendLine(" <LI>Le dossier <I>resfiles</I>, qui contient les ressources du projet, comme les images du site Web interne ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>flplayer</I>, qui contient les fichiers du lecteur Flash Player, au cas où il serait activé ;</LI>")
            patternpage.AppendLine(" <LI>Le dossier <I>emojis</I> stocke les fichiers image qui représentent des émojis, c'est-à-dire les petites images pour représenter des humeurs, entre autres ;</LI>")
            patternpage.AppendLine(" <LI>Les fichiers <I>YTSrv.deps.json</I>, et <I>YTSrv.runtimeconfig.json</I> qui sont des scripts json vitaux pour que les binaires fonctionnent ;</LI>")
            patternpage.AppendLine(" <LI>Les fichiers <I>YTSrv.dll</I> et <I>YTSrv.pdb</I>, générés par Visual Basic .NET et indispensables au fonctionnement du logiciel ;</LI>")
            patternpage.AppendLine(" <LI><I>ffmpeg.exe</I> mis par les soins de l'utilisateur dans le dossier du proxy. Il s'agit d'un programme crucial qui permet de convertir à la volée les fichiers vidéo téléchargés vers un format compatible avec les anciennes configurations ;</LI>")
            patternpage.AppendLine(" <LI><I>yt-dlp.exe</I> mis par les soins de l'utilisateur, également dans le dossier du proxy. Il permet d'obtenir des vidéos depuis YouTube ;</LI>")
            patternpage.AppendLine(" <LI><I>ImageTool.exe</I>, <I>ImageT.deps.json</I>, <I>ImageT.dll</I>, <I>ImageT.pdb</I>, et <I>ImageT.runtimeconfig.json</I> sont nécessaires pour traiter les miniatures en cache, ajouter du texte dessus (comme le compteur de durée) et les redimensionner ;</LI>")
            patternpage.AppendLine(" <LI><I>RetroYT.exe</I> qui est le fichier binaire de lancement du logiciel lui-même.</LI>")
            patternpage.AppendLine("</UL>")
            patternpage.AppendLine()

            patternpage.AppendLine("La suppression de ces éléments empêcherait le démarrage ou le fonctionnement correct du proxy. Si le serveur est fermé pendant la conversion d'un ou plusieurs fichiers vidéo, sachez que des fichiers temporaires nommés <I>output_xxxx.lock</I> (où xxxx est un hash MD5 unique) sont générés avant le début de la conversion. Au cas où vous redémarreriez le logiciel, ces fichiers contiennent le(s) identifiant(s) des processus de ffmpeg.exe dernièrement lancés, ainsi que les fichiers qui étaient en cours de traitement. Ainsi, les processus fantômes de ffmpeg seront coupés, les fichiers temporaires seront supprimés, ainsi que les fichiers vidéo dont les conversions ont été inaccomplies, pour éviter tout fichier corrompu et tout plantage. Idem pour les fichiers en cours de téléchargement avec <I>download_xxxxxx.lock</I> où xxxxxx est un hash MD5 unique.<BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("Si, côté client, les recherches n'affichent aucun résultat quel que soit le mot-clé renseigné, cela peut venir du fait que yt-dlp n'est pas reconnu par YouTube comme un navigateur web classique, mais comme un trafic automatisé (un «&nbsp;bot&nbsp;»). Dans ce cas, YouTube peut limiter ou bloquer les requêtes de recherche effectuées de façon anonyme. Pour contourner ce problème, vous pouvez ajouter un fichier ""cookies.txt"" dans le dossier de RetroYT. Celui-ci permet à YT-DLP d'utiliser une session YouTube existante afin d'effectuer les recherches comme si elles provenaient d'un utilisateur déjà connecté, plutôt que d'une session anonyme pouvant être plus limitée. Le fichier cookies.txt peut être exporté depuis votre navigateur web (Firefox, Chrome, Edge, etc.) à l'aide d'une extension dédiée comme «&nbsp;Get cookies.txt LOCALLY&nbsp;». Il ne s'agit pas simplement de copier les fichiers internes du profil Firefox, car leur format n'est pas directement exploitable par YT-DLP. <B>Attention toutefois:</B> si vous partagez ce proxy avec d'autres utilisateurs, ceux-ci n'auront pas accès à votre compte Google ni à vos données personnelles directement, mais les résultats de recherche pourront être influencés par l'activité de votre compte YouTube (historique, préférences, recommandations, personnalisation, etc.). En d'autres termes, les résultats affichés risquent d'être partiellement biaisés par votre propre utilisation préalable de YouTube, et avoir des vidéos adaptées à votre propre activité.<BR><BR>")
            patternpage.AppendLine()

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""configuration"" STYLE=""color: red;"">IV. Configuration</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""configuration"" STYLE=""color: black;"">IV. Configuration</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("Du côté du serveur, il est recommandé d'exécuter RetroYT sur une machine relativement performante. Une connexion Internet stable et rapide est également recommandé. Le transcodage vidéo effectué par FFmpeg peut solliciter fortement le processeur, en particulier lors de l'utilisation de codecs anciens ou peu optimisés comme Cinepak ou MSVideo1. Windows 10 et Windows 11 sont actuellement les systèmes les plus recommandés pour héberger le proxy. Le logiciel nécessite l'environnement .NET 6.0 ou plus, afin de fonctionner correctement. Du côté client, RetroYT a été conçu pour rester accessible à des navigateurs et systèmes beaucoup plus anciens. La navigation sur le proxy ainsi que la lecture vidéo intégrée ont notamment été testées avec succès sur les configurations suivantes&nbsp;:<BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<UL>")
            patternpage.AppendLine(" <LI>Windows NT 4.0 SP6, Internet Explorer 5.5, Windows Media Player 6.4, 1Go de RAM, 32Mo de mém. vidéo et proc. de 700MHz ;</LI>")
            patternpage.AppendLine(" <LI>Windows 2000 SP4, Internet Explorer 6.0, Windows Media Player 9.0, 3Go de RAM, 256Mo de mém. vidéo et proc. de 1,85GHz ;</LI>")
            patternpage.AppendLine(" <LI>Windows XP, Internet Explorer 6.0, Windows Media Player 11.0, 2Go de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows XP, Mozilla Firefox 52.0, Plugin de VLC Media Player 3.0, 2Go de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows ME, Internet Explorer 5.5, Windows Media Player 7.0, 1Go de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows 98 SE, Internet Explorer 4.01, Flash Player 8, 1Go de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows 95 OSR 2.0, Internet Explorer 3.0, ActiveMovie et Media Player, 128Mo de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows 3.11, Internet Explorer 4.01, Real Player 5.0, 64Mo de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows NT 3.51, Internet Explorer 4.01, Real Player 5.0, 64Mo de RAM ;</LI>")
            patternpage.AppendLine(" <LI>MacOS X 7.5.3, NetScape 1.1 et Internet Explorer 4.01, Apple QuickTime 3, 512Mo de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Linux CentOS 6.10, SeaMonkey 2.49.7, Totem et GStreamer, 2Go de RAM ;</LI>")
            patternpage.AppendLine(" <LI>Windows 11, Opera 130.0, Intégration vidéo HTML5 avec 16Go de RAM, 2,8GHz de processeur, et 6Go de mémoire vidéo.</LI>")
            patternpage.AppendLine("</UL><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("Veillez à autoriser l'exécution des contrôles ActiveX, si vous utilisez un système d'exploitation de Microsoft. Veillez aussi à avoir un ou plusieurs lecteurs multimédias installés, et les cookies activés sur votre navigateur. En effet, RetroYT fait usage d'un cookie pour mémoriser les paramètres du client. Si ce dernier ne semble pas prendre en charge les cookies, vous pourrez toujours faire usage des paramètres GET dans l'URL de /watch, /lucky, /short ou /stream. Pour les très anciennes versions de Windows, faire usage du codec MPEG-1, MSVideo1 ou RealMedia depuis la section ""Paramètres"" est recommandé, en résolution 240p et en 15 images/s, tout en veillant à ce que les vidéos ne dépassent pas 10 minutes de longueur. Il s'agit d'un codec avec compression intégrée, totalement compatible avec Windows depuis sa version 3.1. Pour les navigateurs compatibles HTML5, vous pouvez activer l'utilisation du format vidéo MP4, et l'intégration multimédia via la balise &lt;video&gt;.<BR>")
            patternpage.AppendLine("Si vous activez le lecteur Flash Player, seul le format FLV (Flash Video) pourra être lu. Pareil pour Real Player, seul le format Real Media sera lu. Si par malheur aucune de ces options ne fonctionne, vous pouvez également cliquer sur le lien pour lire le flux vidéo directement (lien présent sous le lecteur, si présent). Le navigateur ouvrira un lecteur externe, ou vous proposera de télécharger le fichier pour le lire après. Mais il s'agit d'une option de dernier recours. Concernant le lecteur Windows Media Player, notez bien que l'utilisation des URL n'est prise en charge qu'à partir de la version 6.4.<BR><BR>")
            patternpage.AppendLine()

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""useget"" STYLE=""color: red;"">V. Utilisation du paramètre GET</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""useget"" STYLE=""color: black;"">V. Utilisation du paramètre GET</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("Pour savoir comment utiliser les paramètres GET, afin de pouvoir préciser les paramètres du client sans l'usage des cookies, via l'URL de lecture. Vous pouvez cliquer sur <A HREF=""/useget.htm"">ce lien</A> pour consulter la page dédiée à cette documentation.<BR><BR>")
            patternpage.AppendLine()

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""playlists"" STYLE=""color: red;"">VI. Listes de lecture (Playlists)</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""playlists"" STYLE=""color: black;"">VI. Listes de lecture (Playlists)</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("Les premières versions de RetroYT ne servaient qu'à lire des vidéos YouTube via des flux. Une interface Web a été ajoutée, puis un système de recherche, et une page de <A HREF=""config.cgi"">paramètres</A>. D'autres fonctionnalités ont été ajoutées, en plus d'avoir corrigé des bugs. En plus de pouvoir afficher les chaînes YouTube, avec les flux vidéo, les shorts et les commentaires, RetroYT peut désormais afficher les playlists d'une chaîne voulue. Si vous avez l'identifiant d'une playlist, c'est également faisable. Il suffit d'utiliser playlist.cgi, suivi du paramètre de l'identifiant unique de la playlist, par exemple: <I>http://serveur/playlist.cgi?id=PL5c8gysZPLlm8TqVL5VKX1kqdgT1LUb8_</I><BR><BR>")

            If wanted_skin = "dark" Then
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""credits"" STYLE=""color: red;"">VII. Remerciements</A></H2></CENTER><BR><BR>")
            Else
                patternpage.AppendLine("<BR><CENTER><H2><A NAME=""credits"" STYLE=""color: black;"">VII. Remerciements</A></H2></CENTER><BR><BR>")
            End If

            patternpage.AppendLine("YouTube est une propriété de Google. Il s'agit d'une plateforme de diffusion de vidéos en direct, ou en différé. Ce projet de proxy n'est pas affilié à Google, ni à YouTube.")
            patternpage.AppendLine("Ce logiciel a été développé sous Microsoft Visual Basic .NET 2022. Il fait usage des librairies et binaires ffmpeg, et du projet yt-dlp, que l'utilisateur doit intégrer manuellement au dossier (ils ne sont pas livrés par défaut pour éviter des problèmes de droit d'auteur avec leurs créateurs respectifs, et pour des raisons d'espace de stockage utilisé). En revanche, SWFObject est inclus au projet directement, car sous licence MIT. Il est donc libre de le redistribuer, et permet la lecture des vidéos au format Macromedia Flash lorsque l'utilisateur active cette fonctionnalité. Merci à ceux qui l'ont programmé !<BR>Merci aussi à ChatGPT pour ses astuces de programmation. Sans lui, ce projet n'aurait peut-être jamais vu le jour. Je remercie également LeJarb pour le code d'intégration de Real Player, le code d'intégration pour les anciennes versions d'Android, et pour les consoles de salon/portatives de Nintendo et SONY. Il a aussi participé à l'optimisation de l'usage des codecs (en s'aidant de Léo AI). Je le remercie aussi pour ses divers feedbacks, et pour sa participation active dans l'amélioration du projet. Je remercie aussi Val pour ses tests du logiciel sur des configurations réelles, ainsi qu'à tous ceux qui ont aussi testé sur des configurations anciennes, que je les connaisse ou non. Merci également à vous, l'utilisateur, pour avoir utilisé RetroYT, en espérant qu'il fonctionnera parfaitement sur votre configuration, et qu'il vous procurera entière satisfaction dans l'usage du service YouTube depuis d'anciens systèmes. Voici la page de débug du projet, pour consulter les paramètres du client: <A HREF=""/debug.cgi"">Cliquez ici</A>. Une page pour consulter le cache (uniquement disponible pour les adresses IP locales) est également disponible ici: <A HREF=""/cache.cgi"">Cliquez ici</A>.<BR><BR><I>L'auteur.</I><BR><BR>")
            patternpage.AppendLine()
            patternpage.AppendLine("<A HREF=""/feed"">Cliquez ici pour retourner à l'index</A><BR><BR>")
            patternpage.AppendLine("</DIV></CENTER><DIV CLASS=bodysep></DIV>" & footer)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /useget.htm") Then
            'Afficher la section "Comment utiliser GET" du proxy
            patternpage.AppendLine(InitValues("Utiliser les paramètres GET", , wanted_skin, , used_player, , True))
            WriteLog("Page des informations sur le paramétrage de GET demandée.", , client)

            patternpage.AppendLine("<BR><CENTER><H1 CLASS=""black_label"">Comment utiliser les paramètres GET</H1></CENTER><BR>")
            patternpage.AppendLine("<CENTER><DIV STYLE=""display: block; width: 780px; margin-left: auto; margin-right: auto; text-align: left; text-align: justify;"">")
            patternpage.AppendLine("Si les cookies ne fonctionnent pas sur votre navigateur, et/ou que vous ne pouvez pas enregistrer les paramètres, ceux par défaut seront appliqués. Par conséquent, certaines fonctionnalités seront incompatibles avec votre configuration, et vous ne pourrez normalement pas lire les vidéos. Heureusement, RetroYT inclut une fonctionnalité pour remédier à cet éventuel manque. Pour modifier la configuration de la lecture sans passer par les cookies (et la sauvegarde du paramétrage qui utilise une requête POST), vous pouvez ajouter des paramètres GET dans l'URL qui suit le modèle: <I>/watch?v=xxxxxxxxxxx</I>. Ce sont les mêmes attributs que ceux utilisés dans la requête POST ou dans le cookie lui-même. Vous pouvez changer le type de lecteur utilisé, la taille du lecteur, le format vidéo utilisé, le nombre d'images par seconde et la résolution, tout cela via l'URL.<BR><BR><BR>")

            patternpage.AppendLine("<CENTER><B>Le lecteur utilisé dans la page de visualisation se change via l'entête <I>player</I> avec pour paramètre un des éléments suivants&nbsp;:</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER WIDTH=500>")
            patternpage.AppendLine(" <TR><TD>no_integration</TD><TD>Aucune intégration multimédia</TD></TR>")
            patternpage.AppendLine(" <TR><TD>legacy_wmp</TD><TD>Usage du lecteur Windows Media Player 6.4</TD></TR>")
            patternpage.AppendLine(" <TR><TD>wmp</TD><TD>Usage du lecteur Windows Media Player 7.0</TD></TR>")
            patternpage.AppendLine(" <TR><TD>embed</TD><TD>Intégration multimédia par la balise HTML &lt;embed&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>video</TD><TD>Intégration multimédia avec la balise &lt;video&gt; de HTML5</TD></TR>")
            patternpage.AppendLine(" <TR><TD>realplayer</TD><TD>Usage du lecteur Real Player via la balise &lt;embed&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>activex_realplayer</TD><TD>Usage du lecteur Real Player via ActiveX</TD></TR>")
            patternpage.AppendLine(" <TR><TD>embed_vlc</TD><TD>Usage du lecteur VLC via la balise &lt;embed&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vlc</TD><TD>Usage du lecteur VLC via ActiveX</TD></TR>")
            patternpage.AppendLine(" <TR><TD>alt_vlc</TD><TD>Lecteur VLC via ActiveX (Avec un CLSID alternatif)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>quicktime</TD><TD>Usage du lecteur QuickTime via ActiveX</TD></TR>")
            patternpage.AppendLine(" <TR><TD>embed_quicktime</TD><TD>Usage du lecteur QuickTime via la balise &lt;embed&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>flash</TD><TD>Usage du lecteur Flash via Javascript</TD></TR>")
            patternpage.AppendLine(" <TR><TD>embed_flash</TD><TD>Usage du lecteur Flash via la balise &lt;embed&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>activex_flash</TD><TD>Usage du lecteur Flash via ActiveX</TD></TR>")
            patternpage.AppendLine(" <TR><TD>object</TD><TD>Intégration multimédia via la balise HTML &lt;object&gt;</TD></TR>")
            patternpage.AppendLine(" <TR><TD>alt_video</TD><TD>Intégration multimédia via la balise &lt;video&gt; de HTML5, mais adaptée aux configurations alternatives (Les anciens smartphones qui exécutent Android 2.x, 3.x, 4.x, les consoles de salon (et portatives) de Nintendo, SONY, etc. connectées à Internet).</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<CENTER><B>La taille du lecteur multimédia intégré dans la page de visualisation se règle avec l'entête <I>size</I>, suivi par un des paramètres suivants&nbsp;:</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER>")
            patternpage.AppendLine(" <TR><TD>auto</TD><TD>Taille du lecteur gérée par Javascript (Qui doit être disponible et activé)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>micro</TD><TD>Taille du lecteur 160x120 pixels (Pour les écrans de portables)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>ultrasmall</TD><TD>Taille du lecteur 256x192 pixels</TD></TR>")
            patternpage.AppendLine(" <TR><TD>small</TD><TD>Taille du lecteur 320x240 pixels (Pour les écrans VGA de base)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>middle</TD><TD>Taille standard VGA, de 640x480 pixels</TD></TR>")
            patternpage.AppendLine(" <TR><TD>large</TD><TD>Taille 854x480 pixels (Format petit cinéma 16:9)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>cinema</TD><TD>Taille 1280x720 pixels (Format large cinéma standard en 16:9)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>bigcinema</TD><TD>Taille 2560x1440 pixels (Format large cinéma de grande taille en 16:9)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>autoheight</TD><TD>Taille du lecteur basée sur la taille de la vidéo (en 4:3)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>fullscreen</TD><TD>Taille du lecteur sur toute la fenêtre (avec HTML)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>fulljs</TD><TD>Taille du lecteur sur toute la zone visible (avec Javascript)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>cs</TD><TD>La taille du lecteur classique YouTube en 480x360 pixels (Taille par défaut)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vertical1</TD><TD>Taille verticale petite, en 270x480 pixels, format 9:16</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vertical2</TD><TD>Taille verticale moyenne, en 360x640 pixels, format 9:16</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vertical3</TD><TD>Taille verticale grande, en 720x1280 pixels, format 9:16</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<CENTER><B>La taille du lecteur multimédia vertical intégré dans la page de lecture des vidéos de type ""shorts"" se règle avec l'entête <I>vsize</I>, suivi par un des paramètres suivants (à noter qu'aucun format 4:3, 16:9 ou 3:4 n'est disponible). La lecture verticale est indisponible pour les codecs suivants: MSVideo1, MPEG-1 100% compatible (la variante dite ""récente"" est compatible), et Cinepak. Le codec RealMedia est disponible pour la lecture des shorts uniquement en résolution 144x256, rendant la navigation possible dans les shorts sous Windows 3.11 :</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER>")
            patternpage.AppendLine(" <TR><TD>vert0</TD><TD>Taille verticale micro en 9:16 (144x256)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vert1</TD><TD>Taille verticale classique en 9:16 (270x480)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vert2</TD><TD>Taille verticale moyenne 9:16 (360x640)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>vert3</TD><TD>Taille verticale grande 9:16 (720x1280)</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<CENTER><B>Le format vidéo utilisé pour la lecture a pour entête <I>codec</I>, qui est accompagnée d'un des paramètres suivants&nbsp;:</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER>")
            patternpage.AppendLine(" <TR><TD>mpeg1</TD><TD>Choix du format MPEG, 100% compatible.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>recent_mpeg1</TD><TD>Choix du format MPEG, codec vidéo MPEG-1, codec audio MP2. Format plus léger que le premier en MPEG-1, mais toujours très rétrocompatible.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>avi_mpeg4</TD><TD>Choix du format AVI (Microsoft), codec vidéo MPEG-4, codec audio MP3. Conseillé sous Windows 98SE.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>avi_msvideo1</TD><TD>Choix du format AVI (Microsoft), codec vidéo MSVideo1, codec audio PCM. Conseillé sous Windows 95, NT, 98, etc.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>avi_cinepak</TD><TD>Choix du format AVI (Microsoft), codec vidéo Cinepak, codec audio PCM. Lent à encoder mais très compatible.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>avi_mjpeg</TD><TD>Choix du format AVI (Microsoft), codec vidéo MJPEG, codec audio PCM. Un format ancien un peu lourd, mais très compatible.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>avi_yuv</TD><TD>Choix du format AVI (Microsoft), vidéo en YUV, codec audio PCM. Format très brut et très lourd, il est vivement recommandé de ne pas dépasser quelques minutes de lecture.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mp4</TD><TD>Choix du format MP4, codec vidéo H.264, codec audio AAC. Compatible avec tous les systèmes à partir du début des années 2000.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>legacy_mp4</TD><TD>Choix du format MP4, codec vidéo H.264, codec audio AAC. Celui-ci est adapté pour les vieux lecteurs (comme celui sous Android 2.2).</TD></TR>")
            patternpage.AppendLine(" <TR><TD>wmv1</TD><TD>Choix du format WMV, codec vidéo WMV1, codec audio WMAv1. Conseillé pour Windows 95, 98, NT avec les codecs installés.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>wmv2</TD><TD>Choix du format WMV, codec vidéo WMV2, codec audio WMAv2. Compatible avec Windows 98SE et plus.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mov_cinepak</TD><TD>Choix du format Apple QuickTime (MOV), codec vidéo Cinepak, codec audio PCM. Compatible avec les MacOS des années 90. Lent à encoder.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mov_svq1</TD><TD>Choix du format Apple QuickTime (MOV), codec vidéo Sorenson SVQ1, codec audio MP3. Compatible avec les MacOS X sortis à partir de 1999.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mov_mpeg4</TD><TD>Choix du format Apple QuickTime (MOV), codec vidéo MPEG-4, codec audio MP3.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mov_rpza</TD><TD>Choix du format Apple QuickTime (MOV), codec vidéo RPZA, codec audio PCM. Compatible avec les MacOS des années 90.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>mov_mjpeg</TD><TD>Choix du format Apple QuickTime (MOV), codec vidéo MJPEG, codec audio PCM. Format assez facile à décoder, très compatible et rapide à convertir.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>3gp</TD><TD>Choix du format 3GP (3G Video), codec vidéo H.263, codec audio AMR Narrowband. Bon pour les portables et smartphones anciens.</TD></TR>")
            patternpage.AppendLine(" <TR><TD>flv</TD><TD>Format Flash Video, codec vidéo Sorenson Spark, codec audio MP3</TD></TR>")
            patternpage.AppendLine(" <TR><TD>rm</TD><TD>Format Real Media, codec vidéo RV10, codec audio AC3. Conseillé pour Windows 3.x et Windows NT 3.x, avec Real Player 5.0 d'installé.</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<CENTER><B>Le nombre d'images est paramétré avec l'entête <I>framerate</I> suivi du nombre d'images voulues UNIQUEMENT parmi cette liste&nbsp;:</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER>")
            patternpage.AppendLine(" <TR><TD>auto</TD><TD>Meilleur nombre d'images par seconde pour le format vidéo voulu [Par défaut].</TD></TR>")
            patternpage.AppendLine(" <TR><TD>10</TD><TD>10 images par seconde (Pour les très vieux ordinateurs)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>12</TD><TD>12 images par seconde</TD></TR>")
            patternpage.AppendLine(" <TR><TD>15</TD><TD>15 images par seconde (Bon rapport qualité/quantité pour les vieux ordinateurs)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>20</TD><TD>20 images par seconde</TD></TR>")
            patternpage.AppendLine(" <TR><TD>24</TD><TD>24 images par seconde (Standard)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>25</TD><TD>25 images par seconde</TD></TR>")
            patternpage.AppendLine(" <TR><TD>30</TD><TD>30 images par seconde</TD></TR>")
            patternpage.AppendLine(" <TR><TD>60</TD><TD>60 images par seconde (Totalement déconseillé pour les anciens PC, et pas toujours disponible)</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("<CENTER><B>La résolution de la vidéo, intitulée <I>resolution</I>, peut être choisie parmi les paramètres suivants&nbsp;:</B><BR><BR>")
            patternpage.AppendLine("<TABLE BORDER=1 CELLPADDING=4 CELLSPACING=0 ALIGN=CENTER>")
            patternpage.AppendLine(" <TR><TD>auto</TD><TD>Meilleure résolution choisie par le serveur, pour chaque format voulu</TD></TR>")
            patternpage.AppendLine(" <TR><TD>96p</TD><TD>Résolution minimale, surtout utile pour le format 3GP</TD></TR>")
            patternpage.AppendLine(" <TR><TD>120p</TD><TD>Résolution très faible</TD></TR>")
            patternpage.AppendLine(" <TR><TD>144p</TD><TD>Résolution faible</TD></TR>")
            patternpage.AppendLine(" <TR><TD>240p</TD><TD>Petite résolution (Recommandée pour toutes les configurations anciennes)</TD></TR>")
            patternpage.AppendLine(" <TR><TD>360p</TD><TD>Moyenne résolution</TD></TR>")
            patternpage.AppendLine(" <TR><TD>480p</TD><TD>Résolution standard</TD></TR>")
            patternpage.AppendLine(" <TR><TD>720p</TD><TD>Haute résolution [HD]</TD></TR>")
            patternpage.AppendLine(" <TR><TD>1080p</TD><TD>Très haute résolution [HD] (Pour les PC de la génération de Windows Vista et plus)</TD></TR>")
            patternpage.AppendLine("</TABLE></CENTER><BR><BR>")
            patternpage.AppendLine()

            patternpage.AppendLine("Sacrées listes, n'est-ce pas? Certaines résolutions seront indisponibles sous certains formats. Pareil pour le nombre d'images. Ceci pour des raisons de limitations techniques fixées par les créateurs du codec, ou pour éviter toute saturation de la mémoire. Les paramètres erronés ou inexistants seront ignorés. Pour illustrer un usage concret de cette fonctionnalité&nbsp;:<BR><BR>")
            patternpage.AppendLine()
            patternpage.AppendLine("<I>http://127.0.0.1/watch?v=dQw4w9WgXcQ&player=video&size=auto&codec=mp4&framerate=24&resolution=480p</I><BR><BR>")
            patternpage.AppendLine("Visiter cette URL démarrera la lecture de la vidéo indiquée au format MP4 (Résolution 480p @ 24 FPS), via le lecteur vidéo intégré de HTML5. La taille du lecteur sera automatiquement réglée. Cette configuration par défaut est très utile pour les navigateurs prenant en charge le HTML5.<BR><BR>")
            patternpage.AppendLine("Tous les paramètres ne sont pas obligatoires. Ainsi, pour démarrer, par exemple, une lecture avec le lecteur Flash :<BR><BR>")
            patternpage.AppendLine("<I>http://127.0.0.1/watch?v=ZyhrYis509A&player=flash&codec=flv&resolution=240p</I><BR><BR>")
            patternpage.AppendLine("Le tout en 240p, avec le nombre d'images par seconde par défaut. Le reste des paramètres utiliseront ceux par défaut également. Cette configuration reste assez typique de l'époque de Flash Player, dans les années 2000.<BR><BR>")
            patternpage.AppendLine("Si vous lisez depuis Windows 3.11 ou Windows NT 3.51, je vous conseille d'installer Real Player 5.0, qui rendra possible la lecture sous Internet Explorer 4 ou 5 via intégration ou ActiveX. Les paramètres à utiliser seront ainsi :<BR><BR>")
            patternpage.AppendLine("<I>http://127.0.0.1/watch?v=FuOhQZP821o&player=realplayer&codec=rm&resolution=240p&framerate=15</I><BR><BR>")
            patternpage.AppendLine("Ce ne sont que des exemples, mais ils vous inspireront probablement pour votre configuration. Faites-en bon usage.<BR><BR>Cliquez <A HREF=""/feed"">ici</A> pour revenir à l'index, et <A HREF=""about.htm"">ici</A> pour revenir à la documentation principale.</DIV><BR><BR>")
            patternpage.AppendLine()
            patternpage.AppendLine(footer)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /e_") Then
            'Obtention des émojis
            request = request.Replace("../", String.Empty) 'Retirer les tentatives de consulter ce qui se situe dans le dossier parent
            request = request.Replace("./", String.Empty)
            request = request.Replace("/.", String.Empty)
            request = request.Replace("/..", String.Empty)
            request = request.Replace("..\", String.Empty)
            request = request.Replace(".\", String.Empty)
            request = request.Replace("\.", String.Empty)
            request = request.Replace("\..", String.Empty)

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 1).Trim

            Dim fs As System.IO.FileStream = Nothing
            Dim resBuffer(8191) As Byte
            Dim resread As Integer = 0

            If arg.EndsWith(".gif") Then

                'Fichier introuvable
                If Not IO.File.Exists(CurDir() & "\emojis\" & arg) Then
                    WriteLog("Ressource demandée introuvable: " & arg, , client)

                    Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Le fichier '<I>/" & arg & "</I>' n'a pas été trouvé sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                    Try
                        stream.Write(notfound_data, 0, notfound_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        client.Close()
                        Exit Sub
                    End Try

                    client.Close()
                    Exit Sub
                End If

                Dim sent_res As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf
                Dim sent_data As Byte() = Nothing

                Try
                    sent_res &= "Content-Type: image/gif" & vbCrLf
                    sent_res &= "Connection: close" & vbCrLf
                    sent_res &= "Accept-Ranges: bytes" & vbCrLf
                    sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                    sent_res &= "Content-Length: " & FileLen(CurDir() & "\emojis\" & arg).ToString & vbCrLf & vbCrLf
                    sent_data = iso.GetBytes(sent_res)

                    stream.Write(sent_data, 0, sent_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                fs = New System.IO.FileStream(CurDir() & "\emojis\" & arg, IO.FileMode.Open, IO.FileAccess.Read)

                Do
                    resread = fs.Read(resBuffer, 0, resBuffer.Length)
                    If resread = 0 Then Exit Do

                    Try
                        stream.Write(resBuffer, 0, resread)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                        fs.Close()
                        client.Close()
                        Exit Sub
                    End Try
                Loop

                fs.Close()
                client.Close()
                Exit Sub
            Else
                If arg.Length > 40 Then
                    arg = arg.Substring(0, 40) & "..."
                End If

                WriteLog("Ressource demandée introuvable: " & arg, , client)

                Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Le fichier '<I>/" & arg & "</I>' n'a pas été trouvé sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    client.Close()
                    Exit Sub
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /") Then
            'Autres requêtes GET
            request = request.Replace("../", String.Empty) 'Retirer les tentatives de consulter ce qui se situe dans le dossier parent
            request = request.Replace("./", String.Empty)
            request = request.Replace("/.", String.Empty)
            request = request.Replace("/..", String.Empty)
            request = request.Replace("..\", String.Empty)
            request = request.Replace(".\", String.Empty)
            request = request.Replace("\.", String.Empty)
            request = request.Replace("\..", String.Empty)

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

            Dim param As String = String.Empty
            Dim arg_o As String = Split(request)(1)
            arg_o = arg_o.Remove(0, 1)

            Dim fs As System.IO.FileStream = Nothing
            Dim resBuffer(8191) As Byte
            Dim resread As Integer = 0

            If arg_o.Length = 0 Then
                'Index du site

                Dim result_page As String = "<TITLE>Found page</TITLE><BODY><H1>302 Page trouvée</H1><P>Pour vous rendre à l'index du site, veuillez cliquer <A HREF=""/feed"">ici</A>.</P></BODY>" & vbCrLf

                Dim index_resp As String = "HTTP/1.1 302 Found" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                        "Location: /feed" & vbCrLf &
                        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                End Try

                client.Close()
                Exit Sub

            Else
                'Ressource hardcodée ou hébergée
                'WriteLog("Fichier demandé par le client: " & arg, , client)

                Dim sent_res As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf
                Dim sent_data As Byte()

                If arg_o.Contains("?") Then
                    param = arg_o.Remove(0, arg_o.IndexOf("?") + 1)
                    arg_o = arg_o.Substring(0, arg_o.IndexOf("?"))
                End If

                If arg_o.Contains("/..") Then arg_o = arg_o.Replace("/..", String.Empty)
                If arg_o.Contains("../") Then arg_o = arg_o.Replace("../", String.Empty)
                If arg_o.Contains("/.") Then arg_o = arg_o.Replace("/.", String.Empty)
                If arg_o.Contains("./") Then arg_o = arg_o.Replace("./", String.Empty)

                If arg_o.Contains("\..") Then arg_o = arg_o.Replace("\..", String.Empty)
                If arg_o.Contains("..\") Then arg_o = arg_o.Replace("..\", String.Empty)
                If arg_o.Contains("\.") Then arg_o = arg_o.Replace("\.", String.Empty)
                If arg_o.Contains(".\") Then arg_o = arg_o.Replace(".\", String.Empty)

                Select Case LCase(arg_o)
                    Case "feed"
                        WriteLog("L'utilisateur demande l'index du site. Renvoi vers la page d'accueil.", , client)
                        patternpage.AppendLine(InitValues("Accueil", , wanted_skin, , used_player))

                        If Not display_trends Then
                            patternpage.AppendLine(" <P ALIGN=CENTER><BR><B>Pour commencer, veuillez entrer un mot-clef à rechercher dans la zone ci-dessus.<BR><BR>Cliquez <A HREF=""/about.htm"">ici</A> pour plus d'informations. Cliquez <A HREF=""/about.htm"">ici</A> pour accéder aux paramètres.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV>")
                            patternpage.AppendLine()
                            patternpage.AppendLine("<BR><BR>" & footer)

                            'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                            Dim req_resp As String =
                                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                                "Connection: close" & vbCrLf &
                                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                            'Conversion en octets, suivant le format ISO-8859-1.
                            Dim req_data As Byte() = iso.GetBytes(req_resp)

                            Try
                                'Ecriture dans le flux octal en direction du client.
                                stream.Write(req_data, 0, req_data.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        Else
                            If IsNetworkAvailable() Then
                                WriteLog("Affichage des tendances activé. Envoi du flux principal...", ConsoleColor.Blue, client)
                                Dim arg As String = Split(request)(1)

                                'Les caractères systèmes sont retirés par sécurité
                                For i As Integer = 0 To &H1F
                                    request = request.Replace(Chr(i), String.Empty)
                                Next

                                'Récupérer les 10 vidéos en rapport avec le mot-clef spécifié
                                'Lancement de yt-dlp
                                Dim output As String = String.Empty

                                Select Case param
                                    Case "cat=music"
                                        output = index_streams(VideoCategories.Musique)
                                    Case "cat=sports"
                                        output = index_streams(VideoCategories.Sports)
                                    Case "cat=gaming"
                                        output = index_streams(VideoCategories.Gaming)
                                    Case "cat=education"
                                        output = index_streams(VideoCategories.Education)
                                    Case "cat=movies"
                                        output = index_streams(VideoCategories.Films)
                                    Case "cat=podcasts"
                                        output = index_streams(VideoCategories.TVSeries)
                                    Case "cat=news"
                                        output = index_streams(VideoCategories.Nouvelles)
                                    Case "cat=entertainment"
                                        output = index_streams(VideoCategories.Divertissement)
                                    Case Else
                                        For i As Integer = 0 To disp_vids_per_channel - 1
                                            Dim sub_video() As String = index_streams(CType(i Mod VideoCategories.Maximum, VideoCategories)).Split("<||>")

                                            For j As Integer = 0 To sub_video.Length - 1
                                                If Not output.Contains(sub_video(j)) Then
                                                    output &= sub_video(j) & "<||>"
                                                    Exit For
                                                End If
                                            Next
                                        Next

                                        If output.EndsWith("<||>") Then output = output.Substring(0, output.Length - 4)
                                End Select

                                'Récupération des lignes
                                If String.IsNullOrEmpty(output) Then
                                    patternpage.AppendLine(" <P ALIGN=CENTER><BR><B>Pour commencer, veuillez entrer un mot-clef à rechercher dans la zone ci-dessus.<BR><BR>Cliquez sur <A HREF=""/about.htm"">ce lien</A> pour obtenir plus d'informations sur le fonctionnement de RetroYT.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV>")
                                    patternpage.AppendLine()
                                Else
                                    If output.EndsWith("<||>") Then output = output.Remove(output.Length - 4, 4)
                                    Dim lines As String() = output.Split("<||>", StringSplitOptions.RemoveEmptyEntries)

                                    If lines.Count = 0 Then
                                        'S'il n'y a aucune ligne retournée, on affiche l'ancien message.
                                        patternpage.AppendLine(" <P ALIGN=CENTER><BR><B>Pour commencer, veuillez entrer un mot-clef à rechercher dans la zone ci-dessus.<BR><BR>Cliquez sur <A HREF=""/about.htm"">ce lien</A> pour obtenir plus d'informations sur le fonctionnement de RetroYT.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV>")
                                        patternpage.AppendLine()
                                    Else
                                        'Sinon, on affiche les résultats dans la page Web.

                                        Select Case param
                                            Case "cat=music"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie musique</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <B>Musique</B> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=sports"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie des sports</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <B>Sports</B> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=gaming"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie gaming</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <B>Gaming</B> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=education"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie éducation</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <B>&Eacute;ducation</B> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=movies"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie films</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <B>Films</B> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=podcasts"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie des podcasts</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <B>Podcasts</B> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=news"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Nouvelles du moment</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <B>Nouvelles</B> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                            Case "cat=entertainment"
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Catégorie divertissement</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><A HREF=""/feed"">Index</A> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <B>Divertissement</B></CENTER></P>")
                                            Case Else
                                                patternpage.AppendLine("<CENTER><P ALIGN=CENTER CLASS=""black_label""><FONT SIZE=4><B>Index du serveur</B></FONT></P></CENTER><BR>")
                                                patternpage.AppendLine("<CENTER><P><B>Index</B> - <A HREF=""/feed?cat=music"">Musique</A> - <A HREF=""/feed?cat=sports"">Sports</A> - <A HREF=""/feed?cat=gaming"">Gaming</A> - <A HREF=""/feed?cat=education"">&Eacute;ducation</A> - <A HREF=""/feed?cat=movies"">Films</A> - <A HREF=""/feed?cat=podcasts"">Podcasts</A> - <A HREF=""/feed?cat=news"">Nouvelles</A> - <A HREF=""/feed?cat=entertainment"">Divertissement</A></CENTER></P>")
                                        End Select

                                        patternpage.AppendLine("<BR><BR>")
                                        patternpage.AppendLine("  <CENTER><TABLE BORDER=0 CELLPADDING=8 CELLSPACING=0 WIDTH=600 ALIGN=CENTER>")
                                        patternpage.AppendLine(" <TR>")

                                        Dim vc As Integer = 0

                                        For Each line In lines

                                            line = line.Replace(vbLf, String.Empty)
                                            line = line.Replace(vbCr, String.Empty)
                                            If line.EndsWith("<|>") Then line = line.Substring(0, line.Length - 3)

                                            Dim parts As String() = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                                            For i As Integer = 0 To parts.Length - 1
                                                For j As Integer = 0 To &H1F
                                                    parts(i) = parts(i).Replace(Chr(j), String.Empty)
                                                Next
                                            Next

                                            If parts.Length = 13 Then
                                                Dim id As String = parts(0)
                                                Dim title As String = parts(1)
                                                Dim tmp_prop As New VideoProperties
                                                title = CleanText(title)

                                                tmp_prop.Title = CleanText(parts(1))

                                                tmp_prop.ID = parts(0)
                                                tmp_prop.Views = IIf(LCase(parts(2)) = "na", "0", GetThousands(parts(2)))
                                                tmp_prop.DateOfRelease = GetDate(parts(3))
                                                tmp_prop.Creator = CleanText(parts(4))
                                                tmp_prop.Thumbnail = parts(5)
                                                tmp_prop.Channel_URL = "/channel.cgi?id=" & CleanText(parts(10))
                                                tmp_prop.Like_Count = parts(11)
                                                tmp_prop.Dislike_Count = parts(12)

                                                If LCase(parts(6)) = "na" Then
                                                    tmp_prop.Duration = -1
                                                Else
                                                    tmp_prop.Duration = CInt(parts(6))
                                                End If

                                                tmp_prop.Dimensions = IIf(IsNumeric(parts(7)), parts(7), "640") & ":" & IIf(IsNumeric(parts(8)), parts(8), "480")

                                                tmp_prop.Description = IIf(String.IsNullOrEmpty(parts(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(parts(9))))
                                                If tmp_prop.Description.Length > 2048 Then tmp_prop.Description = tmp_prop.Description.Substring(0, 2048) & "..."
                                                tmp_prop.Description = tmp_prop.Description.Replace(vbCrLf, "<BR>")
                                                tmp_prop.Description = tmp_prop.Description.Replace(vbCr, "<BR>")
                                                tmp_prop.Description = tmp_prop.Description.Replace(vbLf, "<BR>")
                                                tmp_prop.DateAdded = Now

                                                SyncLock video_props
                                                    If Not video_props.ContainsKey(id) Then
                                                        Try
                                                            If video_props.Count > 1000 Then
                                                                Do Until video_props.Count = 1000
                                                                    video_props.Remove(video_props.Keys(0))
                                                                Loop
                                                            End If

                                                            video_props.Add(id, tmp_prop)
                                                        Catch ex As Exception

                                                        End Try
                                                    End If
                                                End SyncLock

                                                vc += 1

                                                'Affichage d'une ligne dans les recherches, sous la forme d'une miniature accompagnée de quelques métadonnées.
                                                patternpage.AppendLine("  <TD WIDTH=160 VALIGN=TOP CLASS=""survol"">")
                                                patternpage.AppendLine("   <CENTER><A HREF=""/watch?v=" & tmp_prop.ID & """><IMG SRC=""/getpic.cgi?url=" & Uri.EscapeDataString(tmp_prop.Thumbnail) & "&amp;type=thumbnail&amp;duration=" & GetDuration(tmp_prop.Duration).Replace(":", "_") & """ ALT=""" & tmp_prop.ID & """ BORDER=0 WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" /></A></CENTER>")
                                                patternpage.Append("   <P><A HREF=""/watch?v=" & tmp_prop.ID & """>" & tmp_prop.Title & "</A>")
                                                If display_stream_button And Not String.IsNullOrEmpty(tmp_prop.ID) Then patternpage.Append(" <A HREF=""/stream?v=" & tmp_prop.ID & """><IMG SRC=""playbtn.gif"" BORDER=0 ALT=""Flux direct"" /></A>")
                                                patternpage.AppendLine()
                                                patternpage.AppendLine("   <BR>Vidéo publiée par <A HREF=""" & tmp_prop.Channel_URL & """>" & tmp_prop.Creator & "</A><BR>" & tmp_prop.Views.Replace(" ", "&nbsp;") & " vue(s)</P>")
                                                patternpage.AppendLine("  </TD>")
                                                If (vc Mod 3 = 0) Then patternpage.AppendLine(" </TR>" & vbCrLf & vbCrLf & " <TR>")
                                            End If
                                        Next

                                        patternpage = New StringBuilder(patternpage.ToString.Substring(0, patternpage.Length - 6))
                                        patternpage.AppendLine("</TABLE></CENTER>")
                                    End If
                                End If

                                patternpage.AppendLine("<BR><BR>" & footer)

                                'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                                Dim req_resp As String =
                                    "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                                    "Content-Length: " & iso.GetBytes(patternpage.ToString).Length.ToString & vbCrLf &
                                    "Connection: close" & vbCrLf &
                                    "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage.ToString

                                'Conversion en octets, suivant le format ISO-8859-1.
                                Dim req_data As Byte() = iso.GetBytes(req_resp)

                                Try
                                    'Ecriture dans le flux octal en direction du client.
                                    stream.Write(req_data, 0, req_data.Length)
                                Catch ex As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                End Try

                                client.Close()
                            Else
                                Dim notfound_data As Byte() = GetHTTPBytes(500, "<H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet, ainsi, la requête ne peut pas être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                Try
                                    stream.Write(notfound_data, 0, notfound_data.Length)
                                Catch ex As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                End Try

                                client.Close()
                            End If
                        End If
                    Case "yt_logo2.gif", "yt_logo.gif", "yt_modrn.gif", "yt_dark.gif", "yt_rose.gif", "yt_aqua.gif", "yt_mono.gif", "yt_mint.gif", "yt_gold.gif", "cosmic.gif", "playbtn.gif", "th_up.gif", "th_down.gif", "playhead.gif", "s_ena_up.gif", "s_dis_up.gif", "s_ena_dw.gif", "s_dis_dw.gif", "s_dis_ms.gif", "s_ena_ms.gif", "s_stream.gif"
                        'Les logos RetroYT, qui font penser à ceux de YouTube, sont mis au format GIF pour garantir une compatibilité maximale avec les navigateurs anciens.
                        'Aussi cosmic.gif.

                        Try
                            sent_res &= "Content-Type: image/gif" & vbCrLf
                            sent_res &= "Connection: close" & vbCrLf
                            sent_res &= "Accept-Ranges: bytes" & vbCrLf
                            sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                            sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg_o).ToString & vbCrLf & vbCrLf
                            sent_data = iso.GetBytes(sent_res)

                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg_o, IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                fs.Close()
                                client.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        'WriteLog("Ressource '" & arg & "' trouvée et envoyée! (Code HTTP 200)")
                    Case "picplay.jpg", "picshort.jpg"
                        sent_res &= "Content-Type: image/jpeg" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg_o).ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg_o, IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                fs.Close()
                                client.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                    Case "btn_grad.png", "hot_grad.png", "btn_pink.png", "hot_pink.png", "hot_aqua.png", "btn_aqua.png", "btn_mint.png", "hot_mint.png", "btn_gold.png", "hot_gold.png"
                        sent_res &= "Content-Type: image/png" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg_o).ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg_o, IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                fs.Close()
                                client.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                    Case "favicon.ico"
                        'Envoi du fichier favicon.ico (avec un format à l'ancienne)
                        sent_res &= "Content-Type: image/x-icon" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\favicon.ico").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\favicon.ico", IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                fs.Close()
                                client.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        'WriteLog("Ressource '" & arg & "' trouvée et envoyée! (Code HTTP 200)")
                    Case "style.css"
                        'Compilation du fichier style.css (<style> n'est plus utilisé dans les pages Web, car son contenu était affiché sur IE 1.0 / 2.0)
                        Dim sent_css As String = String.Empty
                        sent_css = "html, body {" & vbCrLf

                        Select Case wanted_skin
                            Case "cosmic"
                                sent_css &= " background-color: #eaeaea;" & vbCrLf
                                sent_css &= " color: #000000;" & vbCrLf
                            Case "dark"
                                sent_css &= " background-color: #000000;" & vbCrLf
                                sent_css &= " color: #ffffff;" & vbCrLf
                            Case "rose"
                                sent_css &= " background-color: #f2def2;" & vbCrLf
                                sent_css &= " color: #100010;" & vbCrLf
                            Case "aqua"
                                sent_css &= " background-color: #ecffff;" & vbCrLf
                                sent_css &= " color: #000040;" & vbCrLf
                            Case "mint"
                                sent_css &= " background-color: #e8ffe8;" & vbCrLf
                                sent_css &= " color: #002000;" & vbCrLf
                            Case "sunshine"
                                sent_css &= " background-color: #fffde7;" & vbCrLf
                                sent_css &= " color: #202000;" & vbCrLf
                            Case Else
                                sent_css &= " background-color: #ffffff;" & vbCrLf
                                sent_css &= " color: #000000;" & vbCrLf
                        End Select

                        If wanted_skin = "cosmic" Then sent_css &= " background-image: url('cosmic.gif');" & vbCrLf

                        sent_css &= " font-family: Tahoma, Roboto, Arial, sans-serif;" & vbCrLf
                        sent_css &= " padding: 12px 12px 12px 12px;" & vbCrLf
                        If Not LCase(ua_string).Contains("msie 3.") Then sent_css &= " line-height: 18px;"
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "html, body, table {" & vbCrLf
                        sent_css &= " padding: 0 0 0 0;" & vbCrLf
                        sent_css &= " margin: 0 0 0 0;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "html, body, td, p {" & vbCrLf
                        sent_css &= " font-size: 12px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "select {" & vbCrLf
                        sent_css &= " width: 240px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".bodysep {" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " width: 64px;" & vbCrLf
                        sent_css &= " height: 200px;" & vbCrLf
                        sent_css &= " min-height: 200px;" & vbCrLf
                        sent_css &= " margin-left: auto;" & vbCrLf
                        sent_css &= " margin-right: auto;" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "select, input {" & vbCrLf
                        sent_css &= " border: 1px solid black;" & vbCrLf
                        sent_css &= " padding: 4px 4px 4px 4px;" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "img {" & vbCrLf
                        sent_css &= " border: 0;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".thumbstyle {" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " width: 160px;" & vbCrLf
                        sent_css &= " height: 100px;" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "h1 {" & vbCrLf
                        sent_css &= " font-size: 24px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "h2 {" & vbCrLf
                        sent_css &= " font-size: 18px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".thumbshort {" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " width: 100px;" & vbCrLf
                        sent_css &= " height: 180px;" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a {" & vbCrLf

                        Select Case wanted_skin
                            Case "dark"
                                sent_css &= " color: #e62727;" & vbCrLf
                            Case "cosmic"
                                sent_css &= " color: #1034be;" & vbCrLf
                            Case "rose"
                                sent_css &= " color: #a0046b;" & vbCrLf
                            Case "aqua"
                                sent_css &= " color: #1f38a0;" & vbCrLf
                            Case "monochrome"
                                sent_css &= " color: #606060;" & vbCrLf
                            Case "mint"
                                sent_css &= " color: #358832;" & vbCrLf
                            Case "sunshine"
                                sent_css &= " color: #89800c;" & vbCrLf
                            Case Else
                                sent_css &= " color: #1034be;" & vbCrLf
                        End Select

                        sent_css &= " font-weight: bold;" & vbCrLf
                        If wanted_skin <> "monochrome" Then sent_css &= " text-decoration: none;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a:hover {" & vbCrLf
                        sent_css &= " text-decoration: underline;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "#mainplayer {" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " position: relative;" & vbCrLf
                        sent_css &= " top: 10px;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        If Not old_ie Then sent_css &= " object-fit: center;" & vbCrLf
                        If Not old_ie Then sent_css &= " margin-left: auto;" & vbCrLf
                        If Not old_ie Then sent_css &= " margin-right: auto;" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".red_button {" & vbCrLf

                        Select Case wanted_skin
                            Case "modern"
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-color: #e01425;" & vbCrLf
                            Case "rose"
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-color: rgb(178, 15, 120);" & vbCrLf
                                sent_css &= " background-image: url('btn_pink.png');" & vbCrLf
                            Case "aqua"
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-image: url('btn_aqua.png');" & vbCrLf
                                sent_css &= " background-color: #1f38a0;" & vbCrLf
                            Case "monochrome"
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-color: black;" & vbCrLf
                                sent_css &= " border: 1px solid black;" & vbCrLf
                            Case "mint"
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-image: url('btn_mint.png');" & vbCrLf
                                sent_css &= " background-color: #1fa027;" & vbCrLf
                            Case "sunshine"
                                sent_css &= " color: black;" & vbCrLf
                                sent_css &= " background-image: url('btn_gold.png');" & vbCrLf
                                sent_css &= " background-color: #a09e20;" & vbCrLf
                            Case Else
                                sent_css &= " color: white;" & vbCrLf
                                sent_css &= " background-color: #e01425;" & vbCrLf
                                sent_css &= " background-image: url('btn_grad.png');" & vbCrLf
                        End Select

                        sent_css &= " background-repeat: repeat-x;" & vbCrLf
                        sent_css &= " font-weight: bold;" & vbCrLf
                        sent_css &= " cursor: hand;" & vbCrLf
                        sent_css &= " cursor: pointer;" & vbCrLf
                        sent_css &= " width: 100px;" & vbCrLf
                        sent_css &= " height: 26px;" & vbCrLf
                        sent_css &= " font-size: 12px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".red_button:hover {" & vbCrLf

                        Select Case wanted_skin
                            Case "modern"
                                sent_css &= " background-color: #f82a0d;" & vbCrLf
                            Case "rose"
                                sent_css &= " background-image: url('hot_pink.png');" & vbCrLf
                                sent_css &= " background-color: rgb(230, 1, 153);" & vbCrLf
                            Case "aqua"
                                sent_css &= " background-image: url('hot_aqua.png');" & vbCrLf
                                sent_css &= " background-color: #2949d9;" & vbCrLf
                            Case "monochrome"
                                sent_css &= " text-decoration: underline;" & vbCrLf
                            Case "mint"
                                sent_css &= " background-image: url('hot_mint.png');" & vbCrLf
                                sent_css &= " background-color: #26b630;" & vbCrLf
                            Case "sunshine"
                                sent_css &= " background-image: url('hot_gold.png');" & vbCrLf
                                sent_css &= " background-color: #c4c12c;" & vbCrLf
                            Case Else
                                sent_css &= " background-image: url('hot_grad.png');" & vbCrLf
                                sent_css &= " background-color: #f82a0d;" & vbCrLf
                        End Select

                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".green_toast {" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " border: 2px solid rgb(0, 128, 0);" & vbCrLf
                        sent_css &= " width: 600px;" & vbCrLf
                        sent_css &= " padding: 4px 4px 4px 4px;" & vbCrLf
                        sent_css &= " border-radius: 8px;" & vbCrLf
                        sent_css &= " color: rgb(0, 128, 0);" & vbCrLf
                        sent_css &= " margin-left: auto;" & vbCrLf
                        sent_css &= " margin-right: auto;" & vbCrLf
                        sent_css &= " background-color: rgb(64, 255, 64);" & vbCrLf
                        sent_css &= " background-color: rgba(32, 225, 32, 0.25);" & vbCrLf
                        sent_css &= " !background-color: rgb(64, 255, 64);" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".black_label {" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " margin-left: auto;" & vbCrLf
                        sent_css &= " margin-right: auto;" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= " color: white;" & vbCrLf
                        sent_css &= " padding: 8px 8px 8px 8px;" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " width: 584px;" & vbCrLf
                        sent_css &= " line-height: 24px;" & vbCrLf
                        sent_css &= " }" & vbCrLf & vbCrLf

                        sent_css &= ".survol:hover {" & vbCrLf
                        sent_css &= " background-color: rgb(192, 225, 255);" & vbCrLf
                        sent_css &= " background-color: rgba(192, 225, 255, 0.5);" & vbCrLf
                        sent_css &= " !background-color: rgb(192, 225, 255);" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_res &= "Content-Type: text/css" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & iso.GetBytes(sent_css).Length.ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_css)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        client.Close()
                    Case "debug.cgi"
                        WriteLog("Page de débug rendue au client.", , client)
                        Dim sent_page As String = "<HTML>" & vbCrLf & " <HEAD><TITLE>Debug display</TITLE><STYLE>html, body { line-height: 24px; }</STYLE></HEAD>" & vbCrLf & " <BODY>" & vbCrLf

                        sent_page &= "<BR><CENTER><A HREF=""/feed"">Retour à l'index</A> - <A HREF=""/config.cgi"">Paramètres du client</A> - <A HREF=""/about.htm"">Informations sur RetroYT</A></CENTER><BR><BR>" & vbCrLf

                        sent_page &= "<B>Ressources picturales utilisées par le serveur:</B><BR>" & vbCrLf

                        For Each f As String In IO.Directory.GetFiles(CurDir() & "\resfiles")
                            f = f.Remove(0, CStr(CurDir() & "\resfiles\").Length)
                            Select Case f
                                Case "nopic.jpg", "noavat.jpg", "blank.gif", "noshort.jpg"
                                    sent_page &= "<!-- " & f & " n'est pas une ressource téléchargeable -->" & vbCrLf
                                Case Else
                                    sent_page &= "<IMG SRC=""" & f & """ ALT=""" & f & """ />" & vbCrLf
                            End Select
                        Next

                        sent_page &= "<BR><BR>" & vbCrLf
                        sent_page &= "<B>Cookie utilisateur:</B> " & EscapeHtml(current_cookie) & "<BR>" & vbCrLf
                        sent_page &= "<B>Hôte HTTP:</B> " & EscapeHtml(last_host) & "<BR>" & vbCrLf
                        sent_page &= "<B>Agent utilisateur:</B> " & EscapeHtml(ua_string) & "<BR>" & vbCrLf

                        sent_page &= "<B>Format multimédia utilisé:</B> " & vbCrLf

                        Select Case used_codec
                            Case "mpeg1"
                                'Codec vidéo MPEG-1, audio MP2
                                sent_page &= "Conteneur MPEG (*.mpg) 100% compatible, codec vidéo: MPEG-1 (1,15MBPS de bitrate), codec audio: MP2 (96KBPS, 2 canaux stéréo @ 44,1KHz), tampon mémoire: 320Ko"
                            Case "recent_mpeg1"
                                'Codec vidéo MPEG-1, audio MP2
                                sent_page &= "Conteneur MPEG (*.mpg), version allégée. Codec vidéo: MPEG-1 (1,15MBPS de bitrate), codec audio: MP2 (192KBPS, 2 canaux stéréo @ 44,1KHz)"
                            Case "avi_mpeg4"
                                'Format AVI encodé avec MPEG-4 (codec vidéo assez fonctionnel et compatible avec les systèmes Windows), et MP3.
                                sent_page &= "Conteneur AVI (Microsoft), codec vidéo: MS MPEG4v2 (500KBPS de bitrate), codec audio: MP3 (128KBPS)"
                            Case "avi_yuv"
                                'Format AVI YUV (sans codec) avec PCM
                                sent_page &= "Conteneur AVI (Microsoft), vidéo YUV (YUY2), audio PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "wmv2"
                                'Format WMV, très utilisé sous Windows, depuis Windows 98. Codec WMV2 et WMAv2
                                sent_page &= "Conteneur WMV (Microsoft), codec vidéo: WMV2 (800KBPS de bitrate), codec audio: WMAv2 (128KBPS)"
                            Case "wmv1"
                                'Format WMV ancien, codec WMV2, audio WMAv1.
                                sent_page &= "Conteneur WMV (Microsoft), codec vidéo: WMV1 (500KBPS de bitrate), codec audio: WMAv1 (128KBPS, 1 canal mono @ 44,1KHz)"
                            Case "rm"
                                'Format Real Media (code par Le Jarb aidé de Léo AI). A permis de faire fonctionner la lecture intégrée sous IE 3.0 et Windows 3.11.
                                'Codec vidéo RV10 et audio AC3
                                sent_page &= "Conteneur Real Media (*.rm), codec vidéo: RV10 (640KBPS de bitrate), codec audio: AC3 (128KBPS)"
                            Case "3gp"
                                'Format 3GP (pour les vieux mobiles Nokia, SONY, etc.), codec vidéo H.263, audio AMR-NB
                                sent_page &= "Conteneur 3GP, codec vidéo: H.263 (128KBPS de bitrate), codec audio: AMR Narrowband (12,2KBPS, 1 canal mono @ 8KHz)"
                            Case "mov_cinepak"
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: Cinepak (Indice qualité: 3), codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                                'Format QuickTime (codec vidéo Cinepak, fortement utilisé dans les années 1990, et PCM pour l'audio)
                            Case "mov_svq1"
                                'Format QuickTime (codec vidéo Sorenson SVQ1, surtout utilisé dans les années 2000, et codec audio MP3)
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: Sorenson (SVQ1, indice qualité: 3), codec audio: MP3 (64KBPS)"
                            Case "mov_mpeg4"
                                'Format QuickTime (codec vidéo MPEG-4, audio MP3)
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: MPEG-4 (500KBPS), codec audio: MP3 (128KBPS, 2 canaux stéréo @ 44,1KHz)"
                            Case "mov_mjpeg"
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: MJPEG (Indice qualité: 4), codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "mov_rpza"
                                'Format QuickTime (codec vidéo RPZA, format très Apple des années 1990, et PCM pour l'audio)
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: RPZA, codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "avi_mjpeg"
                                sent_page &= "Conteneur AVI (Microsoft), codec vidéo: MJPEG (Indice qualité: 4), codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "avi_msvideo1"
                                'Format AVI encodé avec Microsoft Video 1 (fonctionne en pratique sous toutes les versions de Windows, y compris Windows 3.11, surtout accompagné du codec audio PCM).
                                sent_page &= "Conteneur AVI (Microsoft), codec vidéo: MSVideo1 (Indice qualité: 3), codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "avi_cinepak"
                                'Format AVI encodé avec Cinepak (codec répandu dans les années 90, et pris en charge par Windows 3.11, surtout accompagné du codec audio PCM).
                                sent_page &= "Conteneur AVI (Microsoft), codec vidéo: Cinepak, codec audio: PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "mp4"
                                'Format MP4 H.264, AAC
                                sent_page &= "Conteneur MP4, codec vidéo: H.264 AVC (MPEG-4 Part 10), codec audio: AAC (128KBPS)"
                            Case "legacy_mp4"
                                sent_page &= "Conteneur MP4, compatible avec les anciens lecteurs (Android par exemple)"
                            Case "xvid"
                                sent_page &= "Conteneur AVI, codec vidéo: Xvid, codec audio: MP3 (128KBPS)"
                            Case "flv"
                                'Format FLV (Codec vidéo Sorenson Spark, audio MP3) [Macromedia Flash Video]
                                sent_page &= "Conteneur Macromedia Flash Video (*.flv), codec vidéo: Sorenson Spark (500KBPS de bitrate), codec audio: MP3 (128KBPS)"
                            Case Else
                                'Par défaut, envoyer du MPEG4.
                                sent_page &= "Conteneur inconnu"
                        End Select

                        sent_page &= "<BR>" & vbCrLf

                        If used_resolution = "auto" Then
                            sent_page &= "<B>Résolution vidéo utilisée:</B> Automatique (En fonction du format multimédia choisi)<BR>" & vbCrLf
                        Else
                            sent_page &= "<B>Résolution vidéo utilisée:</B> " & EscapeHtml(used_resolution) & "<BR>" & vbCrLf
                        End If

                        sent_page &= "<B>Taille du lecteur utilisée:</B> "
                        For i As Integer = 0 To list_playersize.Length - 1
                            If list_playersize(i) = player_size Then
                                sent_page &= EscapeHtml(list_playersize_string(i))
                                Exit For
                            End If
                        Next

                        sent_page &= "<BR>" & vbCrLf

                        sent_page &= "<B>Taille verticale du lecteur (pour les vidéos de type short):</B> "

                        Select Case player_vsize
                            Case "vert0"
                                sent_page &= "Micro (144x256)"
                            Case "vert1"
                                sent_page &= "Petite (270x480)"
                            Case "vert2"
                                sent_page &= "Moyenne (360x640)"
                            Case "vert3"
                                sent_page &= "Grande (720x1280)"
                            Case Else
                                sent_page &= "&lt;Taille inconnue &gt;"
                        End Select

                        sent_page &= "<BR>" & vbCrLf

                        sent_page &= "<B>Nombre d'images par seconde:</B> "

                        If frame_rate = "auto" Then
                            sent_page &= "Automatique (En fonction du format multimédia choisi)<BR>" & vbCrLf
                        Else
                            sent_page &= EscapeHtml(frame_rate) & " images par seconde<BR>" & vbCrLf
                        End If

                        sent_page &= "<B>Intégration multimédia utilisée:</B> "

                        For i As Integer = 0 To list_used_player.Length - 1
                            If list_used_player(i) = used_player Then
                                sent_page &= EscapeHtml(list_used_player_string(i))
                                Exit For
                            End If
                        Next

                        sent_page &= "<BR>" & vbCrLf

                        sent_page &= "<B>Apparence du site utilisée:</B> "

                        For i As Integer = 0 To list_skin.Length - 1
                            If list_skin(i) = wanted_skin Then
                                sent_page &= EscapeHtml(list_skin_string(i))
                                Exit For
                            End If
                        Next

                        sent_page &= "<BR>" & vbCrLf

                        sent_page &= "<B>Nombre de résultats par recherche:</B> " & number_of_results.ToString & " résultat(s)<BR>" & vbCrLf

                        sent_page &= "<B>Affichage du volet des suggestions:</B> " & right_panel.ToString & "<BR>" & vbCrLf

                        sent_page &= "<B>Affichage des tendances YouTube:</B> " & display_trends.ToString & "<BR>" & vbCrLf

                        sent_page &= "<B>Affichage du bouton &quot;Flux direct&quot;:</B> " & display_stream_button.ToString & "<BR>" & vbCrLf

                        sent_page &= "<B>Nombre de commentaires par vidéo:</B> " & disp_comments_per_video.ToString & " commentaire(s)<BR>" & vbCrLf

                        sent_page &= "<B>Nombre de vidéos par flux:</B> " & disp_vids_per_channel.ToString & " vidéos<BR>" & vbCrLf

                        sent_page &= "<B>Empêcher la lecture des vidéos longues (plus d'une heure):</B> " & forbid_long_vids.ToString & "<BR>" & vbCrLf

                        If IO.Directory.Exists(CurDir() & "\emojis") Then
                            sent_page &= "<B>Emojis intégrés à l'application:</B><BR>"
                            For Each f As String In IO.Directory.GetFiles(CurDir() & "\emojis")
                                If f.EndsWith(".gif") Then
                                    Dim final_path As String = Split(f, "\")(Split(f, "\").Length - 1)
                                    sent_page &= "<IMG SRC=""/" & final_path & """ ALT=""" & final_path & """ />&nbsp;"
                                End If
                            Next
                            sent_page &= vbCrLf & "<BR>"
                        End If

                        sent_page &= "<B>Serveur démarré depuis le " & up_since.ToLongDateString & " à " & up_since.ToLongTimeString & "</B>.<BR><BR>" & vbCrLf

                        sent_page &= vbCrLf & "</BODY></HTML>" & vbCrLf & vbCrLf

                        sent_res &= "Content-Type: text/html; charset=iso-8859-1" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & iso.GetBytes(sent_page).Length.ToString & vbCrLf & vbCrLf & sent_page
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        client.Close()
                    Case "swfobject.js"
                        'Envoi du fichier swfobject.js, pour utiliser le lecteur Flash
                        sent_res &= "Content-Type: application/javascript" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\flplayer\swfobject.js").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Select
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\flplayer\swfobject.js", IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                client.Close()
                                fs.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                    Case "player.swf"
                        'Le fichier qui contient le lecteur Flash au format Shockware (Projet SWFObject, sous licence MIT)
                        sent_res &= "Content-Type: application/x-shockwave-flash" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\flplayer\player.swf").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\flplayer\player.swf", IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi du fichier au client: " & ex.Message, ConsoleColor.Red)
                                client.Close()
                                fs.Close()
                                Exit Sub
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        WriteLog("Lecteur Flash requis par l'utilisateur. Envoi immédiat.", , client)
                    Case "teapot.cgi"
                        'Le fichier qui contient le lecteur Flash au format Shockware (Projet SWFObject, sous licence MIT)
                        Dim teapot_page As String = "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 418 - I'm A Teapot</H1><P>La préparation du café demandé par l'utilisateur n'a pu être accomplie, car l'application est une théière.</P>" & vbCrLf

                        sent_res = "HTTP/" & http_ver & " 418 I'm A Teapot" & vbCrLf
                        sent_res &= "Content-Type: text/html" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & iso.GetBytes(teapot_page).Length.ToString & vbCrLf & vbCrLf & teapot_page
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        client.Close()
                        WriteLog("L'utilisateur demande un café, mais l'application est une théière.", ConsoleColor.Magenta, client)
                    Case Else
                        'En cas de ressource introuvable, ou inutilisée par le serveur
                        If arg_o.Length > 40 Then
                            arg_o = arg_o.Substring(0, 40) & "..."
                        End If

                        WriteLog("Ressource demandée introuvable: " & arg_o, , client)

                        Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Le fichier '<I>/" & arg_o & "</I>' n'a pas été trouvé sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                        Try
                            stream.Write(notfound_data, 0, notfound_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        client.Close()
                End Select
            End If
        Else
            'Les autres requêtes entraînent une erreur 400 (requête invalide).
            WriteLog("Erreur HTTP #400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Requête HTTP invalide ou malformée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/feed"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                client.Close()
                Exit Sub
            End Try

            client.Close()
        End If
    End Sub
End Module
