Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime
Imports System.Security.Cryptography
Imports System.Text

Module Program

    'Projet RetroYT codé par Monokeros en 2026
    'Tous droits réservés. Licence freeware/open source.

    Public port As Integer = 80 'Port à écouter pour créer le serveur
    Public patternpage As String = Nothing 'Page HTML modèle à renvoyer au client
    Public video_props As New Dictionary(Of String, VideoProperties)
    Public last_view As String = Nothing 'Identifiant de la vidéo en cours de lecture
    Public iso As Encoding = Encoding.GetEncoding("iso-8859-1")
    Public last_host As String = String.Empty
    Public link_color As String = "#800000"
    Public range_begin As Long = -1
    Public range_end As Long = -1
    Public number_of_vids As Integer = 0
    Public number_of_dls As Integer = 0
    Public sw As New Stopwatch
    Public ip_list As New Dictionary(Of String, Decimal) 'Liste des adresses IP connectés, et du nombre de requêtes par IP.

    Public list_used_player() As String = {"no_integration", "legacy_wmp", "wmp", "embed", "video", "realplayer", "activex_realplayer", "embed_vlc", "vlc", "alt_vlc", "quicktime", "embed_quicktime", "flash", "embed_flash", "activex_flash", "object"}
    Public list_skin() As String = {"oldyt", "cosmic", "dark", "modern", "rose", "aqua", "monochrome"}
    Public list_playersize() As String = {"auto", "micro", "middle", "ultrasmall", "small", "large", "cinema", "bigcinema", "autoheight", "fullscreen", "fulljs", "gold1", "gold2", "cs"}
    Public list_usedcodec() As String = {"mpeg1", "avi_mpeg4", "avi_msvideo1", "avi_mjpeg", "mp4", "rm", "wmv2", "mov_cinepak", "mov_svq1", "3gp", "avi_yuv", "flv", "wmv1", "mov_mpeg4", "avi_cinepak", "mov_rpza", "mov_mjpeg", "xvid"}
    Public list_framerate() As String = {"auto", "10", "12", "15", "20", "24", "25", "30", "60"}
    Public list_resolution() As String = {"auto", "96p", "120p", "144p", "240p", "360p", "480p", "720p", "1080p"}
    Public list_results() As String = {"1", "5", "10", "20"}

    Public list_used_player_string() As String = {"Aucune intégration", "Windows Media Player 6.4 (ActiveX)", "Windows Media Player 7.0 ou plus (ActiveX)", "Intégration générique (Embarquée)", "Intégration vidéo HTML5", "Real Player (Embarqué)", "Real Player (ActiveX)", "Lecteur VLC (Embarqué)", "Lecteur VLC (ActiveX)", "Lecteur VLC (ActiveX avec un CLSID alternatif)", "Apple QuickTime (ActiveX)", "Apple QuickTime (Embarqué)", "Flash Player (Javascript)", "Flash Player (Embarqué)", "Flash Player (ActiveX)", "Intégration générique (Object)"}
    Public list_skin_string() As String = {"Apparence classique", "Cosmic Tube", "Mode sombre", "Apparence moderne", "Thème rose", "Thème aquatique"}
    Public list_playersize_string() As String = {"Automatique (Javascript)", "Taille micro (160x120)", "Taille standard (640x480)", "Taille ultra compacte (256x144)", "Taille compacte (320x240)", "Taille large (854x480)", "Taille cinéma (1280x720)", "Automatique (En fonction du ratio de la vidéo)", "Plein écran (Proportionnellement à la taille du rendu)", "Plein écran (Javascript)", "16:10 Standard (1280x800)", "16:10 Grand (1440x900)"}

    Public http_status_labels(1024) As String

    'Pied de page générique à certaines pages.
    Public footer As String = "<HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & "<P ALIGN=CENTER><B>RetroYT Bêta 5.5</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.<BR>Ce projet n'est pas affilié avec cette entreprise. <A HREF=""/about.htm"" STYLE=""color: " & link_color & """>Plus d'informations sur RetroYT</A>.</P>" & vbCrLf & "<!-- Préchargement des images utilisées par les différents skins -->" & vbCrLf & "<IMG SRC=""btn_aqua.png"" alt=""Button Aqua Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_aqua.png"" alt=""Button Aqua Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_grad.png"" alt=""Button Red Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_grad.png"" alt=""Button Red Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_pink.png"" alt=""Button Pink Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_pink.png"" alt=""Button Pink Hot"" WIDTH=1 HEIGHT=1 />" & vbCrLf & "</BODY>" & vbCrLf & "</HTML>" & vbCrLf
    Public Const cookie_header As String = "retroyt="
    Public vt As RequestVideoType = RequestVideoType.WatchVideo

    Public Enum RequestVideoType
        WatchVideo 'Regarder une vidéo directement, intégrée dans une page HTML
        StreamVideo 'Regarder une vidéo directement, envoyée sous forme de flux
        LuckyVideo 'Chercher un tag, et retourner la première vidéo trouvée
        SearchVideo 'Chercher une vidéo et retourner une liste formatée en une page Web interprétable par un navigateur.
    End Enum

    Public Class VideoProperties
        Public Title As String = "(Titre inconnu)"
        Public Dimensions As String = "640:480"
        Public Description As String = "Aucune description disponible."
        Public Creator As String = "(Créateur inconnu)"
        Public DateOfRelease As String = "1 jan. 1970"
        Public Duration As String = "0:00"
        Public Views As String = "0"
        Public DateAdded As Date = New Date(1970, 1, 1)
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

    Sub InitValues(Optional ByVal t As String = Nothing, Optional ByVal k As String = Nothing, Optional ByVal wanted_skin As String = "cosmic", Optional ByVal lucky As Boolean = False, Optional ByVal uplayer As String = "na", Optional ByVal disp_search As Boolean = True)
        System.Threading.Thread.Sleep(100)
        'Cette fonction génère une entête et un corps de page HTML de base à retourner au client.

        If uplayer = "video" Then
            patternpage = "<!doctype html>" & vbCrLf
        Else
            patternpage = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
        End If

        patternpage &= "<HTML>" & vbCrLf
        patternpage &= " <HEAD>" & vbCrLf

        If t = Nothing Then
            patternpage &= "  <TITLE>RetroYT</TITLE>" & vbCrLf
        Else
            'Echappement des caractères pour éviter les bugs et les injections HTML.
            patternpage &= "  <TITLE>RetroYT - " & EscapeHtml(t) & "</TITLE>" & vbCrLf
        End If

        patternpage &= "  <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">" & vbCrLf
        patternpage &= "  <META CHARSET=""iso-8859-1"" />" & vbCrLf
        patternpage &= "  <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />" & vbCrLf
        patternpage &= "  <LINK REL=""stylesheet"" TYPE=""text/css"" HREF=""style.css"" />" & vbCrLf
        patternpage &= " </HEAD>" & vbCrLf & vbCrLf

        Select Case wanted_skin
            Case "dark"
                patternpage &= "<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" LINK=""#FFFFFF"" ALINK=""#FFFFFF"" VLINK=""#FFFFFF"">" & vbCrLf
                link_color = "#c2272f"
            Case "cosmic"
                patternpage &= "<BODY TEXT=""#000000"" BGCOLOR=""#EAEAEA"" LINK=""#B6262C"" ALINK=""#B6262C"" VLINK=""#B6262C"" BACKGROUND=""cosmic.gif"">" & vbCrLf
                link_color = "#1034be"
            Case "rose"
                patternpage &= "<BODY TEXT=""#100010"" BGCOLOR=""#F2DEF2"" LINK=""#800080"" ALINK=""#800080"" VLINK=""#800080"">" & vbCrLf
                link_color = "#a0046b"
            Case "aqua"
                patternpage &= "<BODY TEXT=""#0000F0"" BGCOLOR=""#ECFFFF"" LINK=""#2037A0"" ALINK=""#2037A0"" VLINK=""#2037A0"">" & vbCrLf
                link_color = "#1f38a0"
            Case "monochrome"
                patternpage &= "<BODY TEXT=""#000000"" BGCOLOR=""#FFFFFF"" LINK=""#606060"" ALINK=""#606060"" VLINK=""#606060"">" & vbCrLf
                link_color = "#606060"
            Case Else
                patternpage &= "<BODY TEXT=""#000000"" BGCOLOR=""#FFFFFF"" LINK=""#B6262C"" ALINK=""#B6262C"" VLINK=""#B6262C"">" & vbCrLf
                link_color = "#1034be"
        End Select

        If Not disp_search Then
            Exit Sub
        End If

        Dim used_logo As String = "yt_logo2.gif"

        Select Case wanted_skin
            Case "oldyt" : used_logo = "yt_logo.gif"
            Case "cosmic" : used_logo = "yt_logo2.gif"
            Case "dark" : used_logo = "yt_dark.gif"
            Case "rose" : used_logo = "yt_rose.gif"
            Case "aqua" : used_logo = "yt_aqua.gif"
            Case "monochrome" : used_logo = "yt_mono.gif"
            Case Else : used_logo = "yt_modrn.gif"
        End Select

        'La tête de page pour rechercher des vidéos. Ce formulaire est présent sur chaque page naviguée.
        patternpage &= " <BR><BR>" & vbCrLf

        If lucky Then
            patternpage &= " <FORM METHOD=""GET"" ACTION=""/lucky"">" & vbCrLf
        Else
            patternpage &= " <FORM METHOD=""GET"" ACTION=""/search"">" & vbCrLf
        End If

        patternpage &= " <CENTER><TABLE BORDER=0 WIDTH=780 ALIGN=CENTER>" & vbCrLf
        patternpage &= "  <TR>" & vbCrLf
        patternpage &= "   <TD WIDTH=90>&nbsp;</TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=120><A HREF=""/""><IMG SRC=""" & used_logo & """ BORDER=0 ALT=""Logo RetroYT"" HEIGHT=44 /></A></TD>" & vbCrLf

        If wanted_skin = "modern" Then
            patternpage &= "   <TD WIDTH=320>&nbsp;<INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 300px;"" WIDTH=300 SIZE=54 MAXLENGTH=256 /></TD>" & vbCrLf
        Else
            patternpage &= "   <TD WIDTH=330>&nbsp;<INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 310px;"" WIDTH=320 SIZE=56 MAXLENGTH=256 /></TD>" & vbCrLf
        End If

        If lucky Then
            patternpage &= "   <TD WIDTH=*><INPUT TYPE=""SUBMIT"" VALUE="" Lucky Mode "" WIDTH=400 CLASS=""red_button"" /> &nbsp; <A HREF=""/config.cgi"" STYLE=""color: " & link_color & ";"">Paramètres</A></TD>" & vbCrLf
        Else
            patternpage &= "   <TD WIDTH=*><INPUT TYPE=""SUBMIT"" VALUE="" Rechercher "" WIDTH=400 CLASS=""red_button"" /> &nbsp; <A HREF=""/config.cgi"" STYLE=""color: " & link_color & ";"">Paramètres</A></TD>" & vbCrLf
        End If

        patternpage &= "  </TR>" & vbCrLf
        patternpage &= " </TABLE></CENTER>" & vbCrLf
        patternpage &= " </FORM><BR><BR>" & vbCrLf & vbCrLf '<HR WIDTH=880 ALIGN=CENTER />

        footer = "<HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & "<P ALIGN=CENTER><B>RetroYT Bêta 5.5</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.<BR>Ce projet n'est pas affilié avec cette entreprise. <A HREF=""/about.htm"" STYLE=""color: " & link_color & ";"">Plus d'informations sur RetroYT</A>.</P>" & vbCrLf & "<!-- Préchargement des images utilisées par les différents skins -->" & vbCrLf & "<IMG SRC=""btn_aqua.png"" alt=""Button Aqua Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_aqua.png"" alt=""Button Aqua Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_grad.png"" alt=""Button Red Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_grad.png"" alt=""Button Red Hot"" WIDTH=1 HEIGHT=1 /><IMG SRC=""btn_pink.png"" alt=""Button Pink Cold"" WIDTH=1 HEIGHT=1 /><IMG SRC=""hot_pink.png"" alt=""Button Pink Hot"" WIDTH=1 HEIGHT=1 />" & vbCrLf & "</BODY>" & vbCrLf & "</HTML>" & vbCrLf
    End Sub

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
        Dim thumbs = Directory.GetFiles(CurDir() & "\thumbs").
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

        text = text.Replace("+", " ")
        text = Uri.UnescapeDataString(text)

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

        If String.IsNullOrEmpty(text) Then
            text = "(Sans titre)"
        End If

        If text.Length = 0 Then
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

        Dim result As String = String.Empty

        While v.Length > 3
            result = " " & v.Substring(v.Length - 3) & result
            v = v.Substring(0, v.Length - 3)
        End While

        result = v & result

        Return result.Trim()
    End Function

    Function EscapeHtml(ByVal h As String) As String
        'Les caractères inutiles ou qui peuvent menacer la sécurité du visualisateur.
        h = h.Replace("<", "&lt;")
        h = h.Replace(">", "&gt;")
        Return h
    End Function

    Function LooksLikeYoutubeID(id As String) As Boolean
        'Si l'ID communiqué est digne de YouTube, ou un truc pipé.
        If id.Length <> 11 Then Return False
        Return System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9_-]+$")
    End Function

    Sub WriteLog(ByVal line As String, Optional ByVal clr As ConsoleColor = ConsoleColor.Gray, Optional ByVal c As TcpClient = Nothing)
        Dim f As String = Nothing

        If (c Is Nothing) Then
            f = "[" & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & "] "
        Else
            f = "[" & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & "] (" & GetClientIP(c) & ") "
        End If

        f &= line

        If clr <> ConsoleColor.Gray Then Console.ForegroundColor = clr
        Console.WriteLine(f)
        Console.ForegroundColor = ConsoleColor.Gray

        Try
            IO.File.AppendAllText("srvlogs\retroyt_server_" & DateTime.Now.ToString("dd-MM-yyyy") & ".log", f & vbCrLf)
        Catch ex As Exception

        End Try
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
        "Cache-Control: no-cache, no-store, must-revalidate" & vbCrLf &
        "Connection: close" & vbCrLf &
        "Pragma: no-cache" & vbCrLf &
        "Expires: 0" & vbCrLf &
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
        http_status_labels(429) = "Too Many Requests"
        http_status_labels(500) = "Internal Server Error"
        http_status_labels(501) = "Not Implemented"
        http_status_labels(502) = "Bad Gateway"
        http_status_labels(507) = "Insufficient Storage"

        'L'application démarre ici!
        Console.Title = "RetroYT"

        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine()
        Console.WriteLine()
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("╔════════════════════════════════════╗")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("║                                    ║")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("║          RetroYT Bêta 5.5          ║")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("║                                    ║")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("║    Copyright (c) 2026 Monokeros    ║")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("║                                    ║")
        Console.Write(Space(Console.WindowWidth / 2 - 19))
        Console.WriteLine("╚════════════════════════════════════╝")
        Console.WriteLine()
        Console.WriteLine()
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

            If portstring = "cross" Then
                'WriteSymbol()
            Else
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
            End If
        Else
            WriteLog("Aucun numéro de port spécifié en ligne de commande. Démarrage sur le port par défaut (80).")
            WriteLog("Pour changer le numéro, lancez RetroYT avec le numéro de port en paramètre immédiat.")
        End If

        'Démarrage du serveur
        Dim listener As New TcpListener(IPAddress.Any, port)

        Try
            listener.Start()
        Catch ex As Exception
            WriteLog("Impossible de démarrer le serveur sur le port spécifié. Raison: " & ex.Message, ConsoleColor.Red)
            Console.ReadKey()
            End
        End Try

        'Création des dossiers nécessaires à l'exécution du programme
        If Not IO.Directory.Exists(CurDir() & "\thumbs") Then
            IO.Directory.CreateDirectory(CurDir() & "\thumbs")
        End If

        If Not IO.Directory.Exists(CurDir() & "\vidcache") Then
            IO.Directory.CreateDirectory(CurDir() & "\vidcache")
        End If

        If Not IO.Directory.Exists(CurDir() & "\srccache") Then
            IO.Directory.CreateDirectory(CurDir() & "\srccache")
        End If

        If Not IO.Directory.Exists(CurDir() & "\srvlogs") Then
            IO.Directory.CreateDirectory(CurDir() & "\srvlogs")
        End If

        If Not IO.Directory.Exists(CurDir() & "\prclocks") Then
            IO.Directory.CreateDirectory(CurDir() & "\prclocks")
        End If

        'Nettoyer les fichiers en cours de décodage
        CleanupLock()
        CleanupDownload()
        UpdateCache()

        WriteLog("Serveur lancé sur le port " & port.ToString & " avec succès! En attente de connexions entrantes...")

        If port = 80 Then
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1/")
        Else
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1:" & port.ToString & "/")
        End If

        WriteLog("Veuillez appuyer sur CTRL+C pour arrêter le serveur.")

        sw.Start()

        While True
            Dim client = listener.AcceptTcpClient()
            Dim t As New Threading.Thread(Sub() HandleClient(client))
            t.Start()

            If sw.ElapsedMilliseconds > 60000 Then
                Try
                    If ip_list.Count > 0 Then
                        For Each p As String In ip_list.Keys.ToList()
                            If ip_list(p) > 300 Then
                                ip_list(p) = -1 'IP bannie, si sous une minute, elle dépasse 200 requêtes HTTP.
                            Else
                                ip_list(p) = 0
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

        Return Encoding.GetEncoding("iso-8859-1").GetString(bytes.ToArray())
    End Function

    Sub HandleClient(client As TcpClient)
        'Variables
        Dim player_size As String = "middle" 'Paramètres par défaut
        Dim used_codec As String = "avi_mpeg4"
        Dim used_player As String = "wmp"
        Dim used_resolution As String = "240p"
        Dim frame_rate As String = "25"
        Dim wanted_skin As String = "cosmic"
        Dim number_of_results As Integer = 10
        Dim http_ver As String = "1.0"
        Dim using_vlc As Boolean = False
        Dim old_ie As Boolean = False
        Dim current_cookie As String = String.Empty
        Dim ua_string As String = String.Empty
        Dim right_panel As Boolean = True

        'Prise en charge des requêtes par le client
        System.Threading.Thread.Sleep(50)
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
                    Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Too Many Requests</H1>" & vbCrLf & "<P>Le serveur a détecté que vous avez envoyé trop de requêtes en une minute.<BR><BR>Votre adresse IP sera donc bannie pour toute cette session.</P>" & vbCrLf)

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
            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête vide</H1>" & vbCrLf & "<P>La requête HTTP est vide, et ne peut donc être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

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
                            If cntnu.Contains(";path=/") Then cntnu = cntnu.Remove(";path=/")
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
                    Case "host"
                        'Dernier nom de domaine exploré par le client
                        last_host = cntnu
                    Case "range"
                        'Reprise d'une lecture en plein milieu en précisant à partir de quel octet reprendre (n'est curieusement jamais appelé par les navigateurs)
                        If LCase(cntnu.StartsWith("bytes=")) Then
                            cntnu = cntnu.Remove(0, 5)
                            If cntnu.Contains(",") Then cntnu = cntnu.Substring(0, cntnu.IndexOf(","))

                            If cntnu = "-" Then
                                Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
            Dim result_page As String = "<H1>Erreur 400 - Requête erronée</H1><P>Le cookie du client était invalide, donc il a été réinitialisé vers les paramètres par défaut.<BR><BR>Veuillez retourner à l'<A HREF=""/"">index</A> du site.</P>" & vbCrLf

            Dim exp As String = DateTime.UtcNow.AddYears(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", Globalization.CultureInfo.InvariantCulture)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 400 Bad Request" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Set-Cookie: " & cookie_header & "results=10&size=cs&codec=avi_mpeg4&player=embed&skin=cosmic&resolution=auto&framerate=auto&panel=true; Path=/; Expires=" & exp & vbCrLf &
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
                    Dim toolongdata As Byte() = GetHTTPBytes(414, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 414 - URI trop longue</H1>" & vbCrLf & "<P>La requête ne peut pas être traitée, car l'URL spécifiée est trop longue.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
            WriteLog("Erreur HTTP #413: Contenu trop grand envoyé.", , client)
            Dim toomuchdata As Byte() = GetHTTPBytes(414, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 413 - Contenu trop grand</H1>" & vbCrLf & "<P>Trop de données communiquées au serveur. Veuillez envoyer moins d'informations.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>La requête HTTP était vide, et ne peut être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /watch?v=") Or request.StartsWith("GET /stream?v=") Then
            'Demande de lecture d'une vidéo par le client
            Dim watcharg As String = Split(request)(1)

            If request.StartsWith("GET /watch?v=") Then
                watcharg = watcharg.Remove(0, 9)
                vt = RequestVideoType.WatchVideo
            Else
                watcharg = watcharg.Remove(0, 10)
                vt = RequestVideoType.StreamVideo
            End If

            UpdateCache()

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
                                single_params(1) = LCase(single_params(1))

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
                    Case "flv" : num_frame_rate = 24
                    Case Else : num_frame_rate = 25
                End Select
            Else
                num_frame_rate = CInt(frame_rate)
            End If

            If used_resolution = "auto" Then
                Select Case used_codec
                    Case "avi_mpeg4", "wmv2", "mov_svq1", "flv", "wmv1", "mov_mpeg4", "xvid" : num_used_resolution = 480
                    Case "avi_msvideo1", "mpeg1", "avi_yuv", "mov_rpza", "avi_mjpeg", "mov_mjpeg" : num_used_resolution = 240
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

            If used_codec = "wmv1" Or used_codec = "mov_svq1" Then
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
                num_used_resolution = 360
                If num_frame_rate = 60 Then num_frame_rate = 30
            End If

            If used_codec = "3gp" Then
                '96p, 120p et 144p uniquement
                If num_used_resolution > 144 Then
                    num_used_resolution = 144
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

            'En fonction du codec/format vidéo demandé, on génère un fichier output_id_000p.ext, où id correspond à l'identifiant de la vidéo YouTube voulue, "000" à la résolution voulue (p = pixels) et "ext" correspond à l'extension.
            Select Case used_codec
                Case "mpeg1" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.mpg"
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
                last_view = watcharg

                WriteLog("Vidéo demandée: https://www.youtube.com/watch?v=" & last_view, ConsoleColor.Green, client)
                'WriteLog("Résolution en " & num_used_resolution.ToString & "p @ " & num_frame_rate.ToString & " FPS demandée.", ConsoleColor.Green)

                If IsNetworkAvailable() Then
                    'Si la vidéo n'est pas en cache, le logiciel va interroger yt-dlp pour l'obtenir.

                    Dim found_video As Boolean = False

                    For Each seek_file As String In IO.Directory.GetFiles(CurDir() & "\srccache")
                        seek_file = seek_file.Remove(0, Convert.ToString(CurDir() & "\srccache").Length + 1)
                        If LCase(seek_file).Contains(LCase(GetMD5(last_view))) Then
                            found_video = True 'Balayer le dossier pour trouver le fichier voulu
                        End If
                    Next

                    If Not found_video Then 'Not IO.File.Exists(output_path)
                        'Exécution du processus d'obtention de la vidéo souhaitée.
                        WriteLog("Téléchargement de la vidéo en cours... Veuillez NE PAS FERMER LA FENÊTRE.", ConsoleColor.DarkRed, client)

                        Dim freeSpace As Long = -1
                        For Each c As IO.DriveInfo In IO.DriveInfo.GetDrives()
                            If LCase(CurDir()).StartsWith(LCase(c.RootDirectory.ToString)) Then
                                freeSpace = c.AvailableFreeSpace
                                Exit For
                            End If
                        Next

                        If freeSpace >= 0 And freeSpace <= 134217728 Then
                            Dim baddata As Byte() = GetHTTPBytes(507, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 507 - Espace disque insuffisant</H1>" & vbCrLf & "<P>Il n'y a plus assez d'espace sur le périphérique de stockage du serveur pour mettre en cache la vidéo demandée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(baddata, 0, baddata.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        Else
                            Dim psi As New ProcessStartInfo()
                            psi.FileName = "yt-dlp.exe"
                            'Dim intermed As Integer = num_used_resolution
                            'If intermed = 120 Then intermed = 144 'Le 120p n'existe pas sur YouTube
                            'If intermed = 96 Then intermed = 144 'Ni le 96p.
                            'If intermed <= 360 Then intermed = 480

                            'Formatage du nom de fichier de destination vers un nom insensible à la casse (usage de l'algorithme MD5) -> ID YouTube vers hash MD5 + extension .dat, qui contiendra MP4 H.264, WebM VP8, VP9, AV1, etc.
                            destfile = CurDir() & "\srccache\" & GetMD5(last_view) 'CurDir() & "\vidcache\" & UCase(GetShortName("output_" & watcharg & "_" & num_used_resolution.ToString & "p.mp4")) & ".MP4"

                            If Not IO.File.Exists(destfile) Then
                                'La commande suivante demande une vidéo au format MP4 (Codec vidéo H.264, audio M4A).
                                Dim lock_file_download As String = CurDir() & "\prclocks\download_" & GetMD5(last_view) & ".lock"

                                If IO.File.Exists(lock_file_download) Then
                                    Dim ise_data As Byte() = GetHTTPBytes(409, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 409 - Demande en conflit</H1>" & vbCrLf & "<P>La vidéo demandée est déjà en cours de téléchargement par le serveur. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                    Try
                                        stream.Write(ise_data, 0, ise_data.Length)
                                    Catch ex As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                Else
                                    'La vidéo est téléchargée en forçant le 1080p
                                    psi.Arguments = "-f ""bv*[height<=1080]+ba/b[height<=1080]"" --no-part --no-continue -o """ & destfile & """ ""https://www.youtube.com/watch?v=" & last_view & """"
                                    psi.UseShellExecute = False
                                    psi.CreateNoWindow = True
                                    psi.RedirectStandardOutput = True
                                    psi.RedirectStandardError = True

                                    If number_of_dls >= 10 Then
                                        Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes en cours</H1>" & vbCrLf & "<P>Il y a déjà 10 vidéos en cours de téléchargement. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                        Try
                                            stream.Write(ise_data, 0, ise_data.Length)
                                        Catch ex As Exception
                                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                        End Try

                                        client.Close()
                                        Exit Sub
                                    End If

                                    'Démarrage du processus de téléchargement, et contrôle du processus via un fichier de verrouillage (en cas de fermeture ou plantage)
                                    Dim p As Process = Process.Start(psi)
                                    Dim has_err As Boolean = False

                                    number_of_dls += 1

                                    Try
                                        IO.File.WriteAllText(lock_file_download, p.Id.ToString & vbCrLf & GetMD5(last_view))
                                    Catch ex As Exception

                                    End Try

                                    Dim output As String = p.StandardOutput.ReadToEnd()
                                    Dim err As String = p.StandardError.ReadToEnd()

                                    Try
                                        p.WaitForExit(1200000)
                                        IO.File.Delete(lock_file_download)
                                    Catch ex As Exception
                                        WriteLog("Erreur lors du processus de téléchargement: " & ex.Message, ConsoleColor.Red)
                                        has_err = True
                                    End Try

                                    If has_err Then
                                        Dim ise_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le traitement de la vidéo demandée n'a pas pu être effectué (Identifiant connu: <I>" & last_view & "</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                                        Try
                                            stream.Write(ise_data, 0, ise_data.Length)
                                        Catch ex As Exception
                                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                                        End Try

                                        client.Close()
                                        Exit Sub
                                    End If

                                    'Affichage du résultat dans la fenêtre
                                    WriteLog(output, ConsoleColor.Cyan)
                                    If Not String.IsNullOrEmpty(err) AndAlso err.Length > 0 Then WriteLog(err, ConsoleColor.Red)

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
                                Dim ise_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>La vidéo demandée n'a pas pu être téléchargée (Identifiant connu: <I>" & last_view & "</I>).<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
                        WriteLog("Vidéo déjà présente dans le cache source !")
                        'WriteLog("Résolution en " & num_used_resolution.ToString & "p @ " & num_frame_rate.ToString & " FPS demandée.", ConsoleColor.Green)
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
                            Dim ise_data As Byte() = GetHTTPBytes(429, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 429 - Trop de requêtes en cours</H1>" & vbCrLf & "<P>Il y a déjà 10 vidéos en cours de traitement. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(ise_data, 0, ise_data.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        End If

                        Dim psi2 As New ProcessStartInfo()
                        psi2.FileName = "ffmpeg.exe"

                        Select Case used_codec
                            Case "mpeg1"
                                'Codec vidéo MPEG-1, audio MP2
                                WriteLog("Conversion du fichier vidéo trouvé vers le format MPEG (Codec vidéo MPEG-1, codec audio MP2)...")
                                num_used_resolution = 360
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=352:240 -r 30000/1001 -c:v mpeg1video -b:v 1150k -maxrate 1150k -minrate 1150k -bufsize 327680 -c:a mp2 -b:a 96k -ar 44100 -ac 2 """ & output_path & """"
                            Case "avi_mpeg4"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI (Codec vidéo MPEG-4, codec audio MP3)...")
                                'Format AVI encodé avec MPEG-4 (codec vidéo assez fonctionnel et compatible avec les systèmes Windows), et MP3.
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 96k """ & output_path & """"
                            Case "avi_yuv"
                                'Format AVI YUV (sans codec) avec PCM
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI (Vidéo YUV, codec audio PCM)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v rawvideo -pix_fmt yuyv422 -vtag YUY2 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                            Case "wmv2"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format WMV nouveau (Codec vidéo WMV2, codec audio WMAv2)...")
                                'Format WMV, très utilisé sous Windows, depuis Windows 98. Codec WMV2 et WMAv2
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv2 -b:v 800k -c:a wmav2 -b:a 96k """ & output_path & """"
                            Case "wmv1"
                                'Format WMV ancien, codec WMV2, audio WMAv1.
                                WriteLog("Conversion du fichier vidéo trouvé vers le format WMV ancien (Codec vidéo WMV1, codec audio WMAv1)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv1 -b:v 500k -c:a wmav1 -b:a 64k -ar 44100 -ac 1 """ & output_path & """"
                            Case "rm"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format RealMedia (Codec vidéo RV10, codec audio AC3)...")
                                'Format Real Media (code par Le Jarb aidé de Léo AI). A permis de faire fonctionner la lecture intégrée sous IE 3.0 et Windows 3.11.
                                'Codec vidéo RV10 et audio AC3
                                If num_used_resolution <= 120 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=160:128 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """"
                                ElseIf num_used_resolution = 144 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """"
                                ElseIf num_used_resolution = 240 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """"
                                Else
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=480:360 -c:a ac3 -r " & num_frame_rate.ToString & " -c:v rv10 -b:v 640k -b:a 64k """ & output_path & """"
                                End If
                            Case "3gp"
                                'Format 3GP (pour les vieux mobiles Nokia, SONY, etc.), codec vidéo H.263, audio AMR-NB
                                WriteLog("Conversion du fichier vidéo trouvé vers le format 3GP (Codec vidéo H.263, codec audio AMR-NB)...")
                                If num_used_resolution = 96 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=128:96 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """"
                                Else
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=176:144 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """"
                                End If
                            Case "mov_cinepak"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format Apple QuickTime (Codec vidéo Cinepak, codec audio PCM)...")
                                'Format QuickTime (codec vidéo Cinepak, fortement utilisé dans les années 1990, et PCM pour l'audio)
                                If num_used_resolution <= 120 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 144 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                Else
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                End If
                            Case "mov_svq1"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format Apple QuickTime (Codec vidéo Sorenson SVQ1, codec audio PCM)...")
                                'Format QuickTime (codec vidéo Sorenson SVQ1, surtout utilisé dans les années 2000, et codec audio MP3)
                                If num_used_resolution >= 720 Then num_used_resolution = 480 'HQ indisponible
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v svq1 -q:v 3 -c:a libmp3lame -b:a 64k """ & output_path & """"
                            Case "mov_mpeg4"
                                'Format QuickTime (codec vidéo MPEG-4, audio MP3)
                                If num_used_resolution >= 720 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo trouvé vers le format Apple QuickTime (Codec vidéo MPEG-4, codec audio MP3)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mpeg4 -b:v 500k -c:a libmp3lame -b:a 96k -ar 44100 -ac 2 """ & output_path & """"
                            Case "mov_mjpeg"
                                'Format QuickTime, encodé MJPEG et PCM
                                If num_used_resolution > 480 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo trouvé vers le format Apple QuickTime, Motion JPEG (Codec vidéo MJPEG, codec audio PCM)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mjpeg -q:v 4 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                            Case "mov_rpza"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format Apple QuickTime (Codec vidéo RPZA, codec audio PCM)...")
                                'Format QuickTime (codec vidéo RPZA, format très Apple des années 1990, et PCM pour l'audio)
                                If num_used_resolution <= 120 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 144 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                Else
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                End If
                            Case "avi_mjpeg"
                                'Format AVI encodé avec MJPEG et PCM
                                If num_used_resolution > 480 Then num_used_resolution = 480 'Bridé à 480p
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI Motion JPEG (Codec vidéo MJPEG, codec audio PCM)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mjpeg -q:v 4 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                            Case "avi_msvideo1"
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI (Codec vidéo MSVideo1, codec audio PCM)...")
                                'Format AVI encodé avec Microsoft Video 1 (fonctionne en pratique sous toutes les versions de Windows, y compris Windows 3.11, surtout puisqu'il accompagné du codec audio PCM).
                                If num_used_resolution <= 120 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 144 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 240 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 360 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=480:360 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                Else
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=640:480 -r " & num_frame_rate.ToString & " -c:v msvideo1 -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                End If
                            Case "avi_cinepak"
                                'Cinepak version AVI, audio PCM
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI (Codec vidéo Cinepak, codec audio PCM)...")
                                'Format AVI encodé avec Cinepak (codec répandu dans les années 90, et pris en charge par Windows 3.11, surtout accompagné du codec audio PCM).
                                If num_used_resolution <= 120 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 144 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                ElseIf num_used_resolution = 240 Then
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                End If
                            Case "mp4"
                                'Format MP4 - Codec vidéo: H.264, codec audio: AAC, avec le format pixel forcé à YUV420P pour éviter les erreurs d'affichage sur les vieux lecteurs. Baseline et level 3.0 avec pour rendre compatible avec les vieux lecteurs Android.
                                WriteLog("Conversion du fichier vidéo trouvé vers le format MP4 (Codec vidéo H.264, codec audio AAC)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v libx264 -preset fast -crf 23 -profile:v baseline -level 3.0 -pix_fmt yuv420p -c:a aac -b:a 128k """ & output_path & """"
                                'psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v libx264 -preset fast -crf 23 -pix_fmt yuv420p -c:a aac -b:a 192k """ & output_path & """"
                            Case "xvid"
                                'Format Xvid, avec le conteneur AVI, et le codec audio MP3
                                WriteLog("Conversion du fichier vidéo trouvé vers le format AVI (Codec vidéo Xvid, codec audio MP3)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v libxvid -qscale:v 3 -vtag xvid -c:a libmp3lame -b:a 128k """ & output_path & """"
                            Case "flv"
                                'Format FLV (Codec vidéo Sorenson Spark, audio MP3) [Macromedia Flash Video]
                                WriteLog("Conversion du fichier vidéo trouvé vers le format vidéo Flash (Codec vidéo Sorenson Spark, codec audio MP3)...")
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v flv -b:v 500k -c:a libmp3lame -b:a 96k """ & output_path & """"
                            Case Else
                                WriteLog("Aucun format de destination valide, conversion de la vidéo vers le format AVI (Codec vidéo MPEG-4, codec audio MP3) par défaut...")
                                'Par défaut, envoyer du MPEG4.
                                psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 96k """ & output_path & """"
                        End Select

                        WriteLog("Veuillez NE PAS FERMER la fenêtre.", ConsoleColor.DarkRed)

                        Dim lock_file_output As String = CurDir() & "\prclocks\output_" & GetMD5(output_path) & ".lock"

                        If IO.File.Exists(lock_file_output) Then
                            'Si le fichier est déjà en cours de conversion
                            Dim ise_data As Byte() = GetHTTPBytes(409, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 409 - Demande en conflit</H1>" & vbCrLf & "<P>La vidéo demandée est déjà en cours de conversion par le serveur. Veuillez réessayer plus tard.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                            Try
                                stream.Write(ise_data, 0, ise_data.Length)
                            Catch ex As Exception
                                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            End Try

                            client.Close()
                            Exit Sub
                        End If

                        psi2.UseShellExecute = False
                        psi2.CreateNoWindow = True

                        number_of_vids += 1

                        Dim p2 As Process = Process.Start(psi2)
                        IO.File.WriteAllText(lock_file_output, p2.Id.ToString & vbCrLf & output_filename)

                        Try
                            p2.WaitForExit(1200000)
                            IO.File.Delete(lock_file_output)
                        Catch ex As Exception
                            WriteLog("Erreur lors de la conversion: " & ex.Message, ConsoleColor.Red)
                        End Try

                        number_of_vids -= 1
                    Else
                        WriteLog("Fichier vidéo de destination déjà existant au format demandé! Aucune conversion nécessaire.", , client)
                    End If

                    'Mise en cache du titre (et de l'ID)
                    Dim tmp_prop As New VideoProperties

                    SyncLock video_props

                        If video_props.Count > 1000 Then
                            Do Until video_props.Count = 1000
                                video_props.Remove(video_props.Keys(0))
                            Loop
                        End If

                        If Not video_props.ContainsKey(watcharg) Then
                            Dim psi3 As New ProcessStartInfo()
                            psi3.FileName = "yt-dlp.exe"
                            psi3.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s"" --no-warnings ""https://www.youtube.com/watch?v=" & watcharg & """ --encoding utf-8"

                            psi3.UseShellExecute = False
                            psi3.RedirectStandardOutput = True
                            psi3.RedirectStandardError = True
                            psi3.CreateNoWindow = True
                            psi3.StandardOutputEncoding = Encoding.UTF8
                            psi3.StandardErrorEncoding = Encoding.UTF8

                            Dim p3 As Process = Process.Start(psi3)
                            Dim output3 As String = p3.StandardOutput.ReadToEnd()
                            Dim err3 As String = p3.StandardError.ReadToEnd()
                            'tmp_title = CleanText(output3)

                            Dim output_elements() As String = Nothing

                            Try
                                output_elements = output3.Split("<|>")

                                For i As Integer = 0 To output_elements.Count - 1
                                    For j As Integer = 0 To &H1F
                                        output_elements(i) = output_elements(i).Replace(Chr(j), String.Empty)
                                    Next
                                Next

                                output_elements(9) = output_elements(9).Replace(vbCrLf, "<BR>")
                                output_elements(9) = output_elements(9).Replace(vbCr, "<BR>")
                                output_elements(9) = output_elements(9).Replace(vbLf, "<BR>")

                                With tmp_prop
                                    .Title = CleanText(output_elements(1))
                                    .Views = IIf(LCase(output_elements(2)) = "na", "0", GetThousands(output_elements(2)))
                                    .DateOfRelease = GetDate(output_elements(3))
                                    .Creator = CleanText(output_elements(4))
                                    .Duration = GetDuration(output_elements(6))
                                    .Dimensions = IIf(IsNumeric(output_elements(7)), output_elements(7), 640) & ":" & IIf(IsNumeric(output_elements(8)), output_elements(8), 480)
                                    .Description = CleanText(output_elements(9))
                                    .DateAdded = Now
                                End With

                                video_props.Add(watcharg, tmp_prop)
                            Catch ex As Exception

                            End Try

                            p3.WaitForExit()
                        Else
                            tmp_prop = video_props(watcharg)
                        End If
                    End SyncLock

                    'Formatage de la page en HTML, avec lecteur intégré

                    If vt = RequestVideoType.WatchVideo Then
                        InitValues(EscapeHtml(tmp_prop.Title), , wanted_skin, , used_player)

                        Dim media_type As String = "video/mp4"

                        Select Case used_codec
                            Case "mp4" : media_type = "video/mp4"
                            Case "rm" : media_type = "application/vnd.rn-realmedia"
                            Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak", "avi_mjpeg", "xvid" : media_type = "video/x-msvideo"
                            Case "wmv1", "wmv2" : media_type = "video/x-ms-wmv"
                            Case "mov_cinepak", "mov_svq1", "mov_mpeg4", "mov_rpza", "mov_mjpeg" : media_type = "video/quicktime"
                            Case "3gp" : media_type = "video/3gpp"
                            Case "mpeg1" : media_type = "video/mpeg"
                            Case "flv" : media_type = "video/x-flv"
                            Case Else : media_type = "application/octet-stream"
                        End Select

                        Dim player_width, player_height As Integer
                        Dim player_prop As String = String.Empty
                        player_width = 640 'Failsafe
                        player_height = 480

                        'Détermination de la taille du lecteur via le cookie
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
                                'Petit lecteur, utile pour les écrans standards des années 1980/1990
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
                                'Lecteur large, format pouvant afficher du 16:9
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
                                patternpage &= "<script language=""javascript"">" & vbCrLf
                                patternpage &= " function resizePlayer() {" & vbCrLf
                                patternpage &= "  var player = document.getElementById(""mainplayer"");" & vbCrLf & vbCrLf

                                patternpage &= "  var winW = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;" & vbCrLf
                                patternpage &= "  var winH = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;" & vbCrLf & vbCrLf

                                patternpage &= "  // Marges" & vbCrLf
                                patternpage &= "  var maxW = winW - 40;" & vbCrLf
                                patternpage &= "  var maxH = winH - 120;" & vbCrLf & vbCrLf

                                patternpage &= "  // Ratio 4:3" & vbCrLf
                                patternpage &= "  var ratioW = " & tmp_w.ToString & ";" & vbCrLf
                                patternpage &= "  var ratioH = " & tmp_h.ToString & ";" & vbCrLf & vbCrLf

                                patternpage &= "  // Calcul basé sur largeur" & vbCrLf
                                patternpage &= "  var width = maxW;" & vbCrLf
                                patternpage &= "  var height = Math.floor(width * ratioH / ratioW);" & vbCrLf & vbCrLf

                                patternpage &= "  // Si ça dépasse en hauteur, alors recalcul de la taille du lecteur" & vbCrLf
                                patternpage &= "  if (height > maxH) {" & vbCrLf
                                patternpage &= "   height = maxH;" & vbCrLf
                                patternpage &= "   width = Math.floor(height * ratioW / ratioH);" & vbCrLf
                                patternpage &= "  }" & vbCrLf & vbCrLf

                                patternpage &= "  // Minimum de 240 pixels de large" & vbCrLf
                                patternpage &= "  if (width < 240) {" & vbCrLf
                                patternpage &= "   width = 240;" & vbCrLf
                                patternpage &= "   height = Math.floor(width * ratioH / ratioW);" & vbCrLf
                                patternpage &= "  }" & vbCrLf & vbCrLf

                                patternpage &= "  player.width = width;" & vbCrLf
                                patternpage &= "  player.height = height;" & vbCrLf
                                patternpage &= " }" & vbCrLf & vbCrLf

                                patternpage &= " window.onload = resizePlayer;" & vbCrLf
                                patternpage &= " window.onresize = resizePlayer;" & vbCrLf
                                patternpage &= "</script>" & vbCrLf & vbCrLf
                                'ChatGPT m'a généré ce code.
                            Case "fulljs"
                                'Plein écran avec Javascript

                                If used_player = "video" Then
                                    patternpage = "<!DOCTYPE html>" & vbCrLf
                                Else
                                    patternpage = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
                                End If

                                patternpage &= "<HTML>" & vbCrLf
                                patternpage &= "<HEAD>" & vbCrLf
                                patternpage &= " <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">" & vbCrLf
                                patternpage &= " <META CHARSET=""iso-8859-1"" />" & vbCrLf
                                patternpage &= " <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />" & vbCrLf
                                patternpage &= "</HEAD>" & vbCrLf & vbCrLf

                                patternpage &= "<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" ALINK=""#C2272F"" VLINK=""#C2272F"" STYLE=""display: block; padding: 0px 0px 0px 0px; margin: 0px 0px 0px 0px;"" TOPMARGIN=0 LEFTMARGIN=0 MARGINHEIGHT=0 MARGINWIDTH=0>" & vbCrLf
                                patternpage &= "<script language=""javascript"">" & vbCrLf
                                patternpage &= " function resizePlayer() {" & vbCrLf
                                patternpage &= "  var player = document.getElementById(""mainplayer"");" & vbCrLf & vbCrLf

                                patternpage &= "  var winW = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;" & vbCrLf
                                patternpage &= "  var winH = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;" & vbCrLf & vbCrLf

                                patternpage &= "  player.width = winW;" & vbCrLf
                                patternpage &= "  player.height = winH;" & vbCrLf
                                patternpage &= " }" & vbCrLf & vbCrLf

                                patternpage &= " window.onload = resizePlayer;" & vbCrLf
                                patternpage &= " window.onresize = resizePlayer;" & vbCrLf
                                patternpage &= "</script>" & vbCrLf & vbCrLf
                                player_prop = "%"
                                'Code de ChatGPT modifié.
                            Case "fullscreen"
                                'Plein écran avec HTML (peut dépasser le cadre)
                                link_color = "#c2272f"

                                If used_player = "video" Then
                                    patternpage = "<!DOCTYPE html>" & vbCrLf
                                Else
                                    patternpage = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
                                End If

                                patternpage &= "<HTML>" & vbCrLf
                                patternpage &= "<HEAD>" & vbCrLf
                                patternpage &= " <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">" & vbCrLf
                                patternpage &= " <META CHARSET=""iso-8859-1"" />" & vbCrLf
                                patternpage &= " <LINK REL=""shortcut icon"" HREF=""favicon.ico"" />" & vbCrLf
                                patternpage &= "</HEAD>" & vbCrLf & vbCrLf

                                patternpage &= "<BODY TEXT=""#FFFFFF"" BGCOLOR=""#000000"" ALINK=""#C2272F"" VLINK=""#C2272F"" STYLE=""display: block; padding: 0px 0px 0px 0px; margin: 0px 0px 0px 0px;"" TOPMARGIN=0 LEFTMARGIN=0 MARGINHEIGHT=0 MARGINWIDTH=0>" & vbCrLf

                                player_width = 100
                                player_height = 100
                                player_prop = "%"
                        End Select

                        'Marge pour les contrôles
                        If used_player <> "video" Then player_height += 30

                        'Titre de la vidéo dans la page
                        Dim actual_width As String = "640"

                        If player_prop = "%" Then
                            actual_width = "100%"
                        Else
                            actual_width = Convert.ToString(Math.Max(480, player_width) + IIf(right_panel, 400, 0))
                        End If

                        If player_size <> "fulljs" AndAlso player_size <> "fullscreen" Then
                            patternpage &= "<CENTER><TABLE BORDER=0 CELLSPACING=0 CELLPADDING=4 ALIGN=CENTER WIDTH=" & actual_width & ">" & vbCrLf
                            patternpage &= " <TR>" & vbCrLf
                            patternpage &= "  <TD COLSPAN=2>" & vbCrLf
                            patternpage &= "   <H2><B>" & EscapeHtml(tmp_prop.Title) & " (<A HREF=""/v/" & output_filename & """ STYLE=""color: " & link_color & ";"">Flux direct</A>)</B></H2>" & vbCrLf
                            patternpage &= "  </TD>" & vbCrLf
                            patternpage &= " </TR>" & vbCrLf

                            patternpage &= " <TR VALIGN=TOP>"
                            patternpage &= "  <TD WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=480>"
                            patternpage &= vbCrLf & "<CENTER>"
                        End If

                        'Le lecteur intégré
                        Select Case used_player
                            Case "legacy_wmp"
                                'Ancien lecteur Windows Media (6.4) intégré avec la balise <object> (ActiveX).
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour Windows Media Player 6.4 -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ CLASSID=""CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""FileName"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""AutoStart"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""EnableFullScreenControls"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""VideoBorder3D"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""StretchToFit"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""ShowControls"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""DisplaySize"" VALUE=4>" & vbCrLf
                                patternpage &= " <PARAM NAME=""DefaultFrame"" VALUE=""" & GetHost() & "thumbnail?t=" & last_view & """>" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf
                            Case "wmp"
                                'Nouveau lecteur Windows Media (7.0 et +) intégré avec la balise <object> (ActiveX).
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour Windows Media Player 7.0 et plus -->" & vbCrLf & vbCrLf

                                If player_prop = "%" Then
                                    patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" WIDTH=""" & player_width.ToString & "%"" HEIGHT=""" & player_height.ToString & "%"" CLASSID=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">" & vbCrLf
                                Else
                                    patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """ CLASSID=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">" & vbCrLf
                                End If

                                patternpage &= " <PARAM NAME=""URL"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""AutoStart"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""EnableFullScreenControls"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""VideoBorder3D"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""StretchToFit"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""ShowControls"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""DefaultFrame"" VALUE=""" & GetHost() & "thumbnail?t=" & last_view & """>" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf
                            Case "vlc"
                                'Lecteur VLC Media Player (via ActiveX)
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur VLC -->" & vbCrLf & vbCrLf

                                If player_prop = "%" Then
                                    patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""CLSID:9BE31822-FDAD-461B-AD51-BE1D1C159921"" WIDTH=""" & player_width.ToString & "%"" HEIGHT=""" & player_height.ToString & "%"">" & vbCrLf
                                Else
                                    patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""CLSID:9BE31822-FDAD-461B-AD51-BE1D1C159921"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """>" & vbCrLf
                                End If

                                patternpage &= " <PARAM NAME=""target"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""MRL"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""autoplay"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""controller"" VALUE=""true"">" & vbCrLf 'Affichage des contrôles du lecteur
                                patternpage &= " <PARAM NAME=""loop"" VALUE=""false"">" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf
                            Case "alt_vlc"
                                'Lecteur VLC Media Player (via ActiveX aussi)
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur VLC avec un identificateur de classe alternatif -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""CLSID:E23FE9C6-778E-49D4-B537-38FCDE4887D8"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""target"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""MRL"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""autoplay"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""controller"" VALUE=""true"">" & vbCrLf 'Affichage des contrôles du lecteur
                                patternpage &= " <PARAM NAME=""loop"" VALUE=""false"">" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf
                            Case "embed_vlc"
                                'Lecteur VLC via la balise HTML embed.
                                patternpage &= "<!-- Embarcation du plugin VLC -->" & vbCrLf & vbCrLf
                                patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" TYPE=""application/x-vlc-plugin"" SRC=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ AUTPLAY=""true"" LOOP=""false"" />" & vbCrLf
                            Case "quicktime"
                                'Lecteur QuickTime via ActiveX (Exclusivement sous Windows)
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur Apple QuickTime. Codebase pointait initialement vers http://www.apple.com/qtactivex/qtplugin.cab, mais le fichier n'est plus disponible. J'ai donc intégré le plugin dans le serveur comme abandonware (Merci à Archive.org pour m'avoir fourni ce fichier!) -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""CLSID:02BF25D5-8C17-4B23-BC80-D3488ABDDC6B"" WIDTH=""" & player_width.ToString & """ HEIGHT=""" & player_height.ToString & """ CODEBASE=""" & GetHost() & "qtplugin.cab"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""autoplay"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""controller"" VALUE=""true"">" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf & vbCrLf
                            Case "embed_quicktime"
                                'Lecteur QuickTime via la balise HTML embed (surtout pour les systèmes Apple)
                                patternpage &= "<!-- Embarcation d'un lecteur Apple QuickTime -->" & vbCrLf & vbCrLf
                                patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ CONTROLLER=""true"" AUTPLAY=""true"" />" & vbCrLf
                            Case "embed"
                                'Balise <embed> générique, une syntaxe et un fonctionnement lancés par NetScape en 1995.
                                patternpage &= "<!-- Embarcation du contenu multimédia avec la balise HTML embed. -->" & vbCrLf & vbCrLf
                                If used_codec = "rm" Then
                                    If player_prop = "%" Then player_height = 90
                                    patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ TYPE=""audio/x-pn-realaudio-plugin"" AUTOSTART=""true"" CONTROLS=""ImageWindow"" CONSOLE=""rmplayer"" /><BR>" & vbCrLf
                                    patternpage &= "<EMBED ALIGN=CENTER WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""20"" TYPE=""audio/x-pn-realaudio-plugin"" CONTROLS=""PositionSlider"" CONSOLE=""rmplayer"" />" & vbCrLf
                                Else
                                    patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ AUTOSTART=""true"" />" & vbCrLf
                                End If
                            Case "video"
                                'Balise <video> de HTML 5.0 (Standard W3C natif aux navigateurs récents)
                                patternpage &= "<!-- Utilisation de la balise video de HTML5 -->" & vbCrLf & vbCrLf
                                patternpage &= "<video id=""mainplayer"" webkit-playsinline controls width=""" & player_width.ToString & player_prop & """ height=""" & player_height.ToString & player_prop & """ autoplay=""true"">" & vbCrLf 'STYLE=""object-fit: fill;""
                                patternpage &= " <source src=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ />" & vbCrLf
                                patternpage &= " <source poster=""" & GetHost() & "thumbnail?t=" & last_view & """ />" & vbCrLf
                                patternpage &= " <P ALIGN=CENTER>Votre navigateur ne semble pas prendre en charge la balise video de HTML5.<BR><BR>Vous pouvez cliquer sur <A HREF=""/config.cgi"">ce lien</A> pour adapter les paramètres de RetroYT à votre configuration.</P>"
                                patternpage &= "</video>" & vbCrLf
                            Case "realplayer"
                                'Intégration du lecteur Real Player (Le code ci-dessous a été créé par Le Jarb, qui s'est appuyé sur Léo AI. Merci pour son implémentation réussie du plugin Real Player, rendant la lecture intégrée sur navigateur possible sous Windows 3.11/NT 3.51)
                                patternpage &= "<!-- Embarcation du lecteur Real Player 5.0 -->" & vbCrLf & vbCrLf
                                If player_prop = "%" Then player_height = 90
                                patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ TYPE=""audio/x-pn-realaudio-plugin"" AUTOSTART=""true"" CONTROLS=""ImageWindow"" CONSOLE=""rmplayer"" /><BR>" & vbCrLf
                                patternpage &= "<EMBED ALIGN=CENTER WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""20"" TYPE=""audio/x-pn-realaudio-plugin"" CONTROLS=""PositionSlider"" CONSOLE=""rmplayer"" />" & vbCrLf
                            'media_type n'est pas précisé en paramètre, car Real Player ne lit que du RealMedia.
                            Case "activex_realplayer"
                                'Real Player (ActiveX)
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour Real Player 5.0 -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""CLSID:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """>" & vbCrLf
                                patternpage &= " <PARAM NAME=""src"" VALUE=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf & vbCrLf
                            Case "no_integration"
                                'Aucune intégration, donc aucun lecteur affiché. Code HTML bidon qui suit.
                                patternpage &= "<!-- Aucune intégration activée --><BR><BR><BR><BR><BR><BR><BR>" & vbCrLf
                            Case "flash"
                                'Lecteur Flash 8 via Javascript
                                patternpage &= "<!-- Intégration d'un lecteur Flash via Javascript -->" & vbCrLf & vbCrLf
                                patternpage &= "<NOSCRIPT><P ALIGN=CENTER>Javascript et Flash Player 8.0 sont nécessaires pour démarrer la lecture.</P></NOSCRIPT>" & vbCrLf & vbCrLf
                                patternpage &= "<SCRIPT LANGUAGE=""javascript"" SRC=""/swfobject.js""></SCRIPT>" & vbCrLf
                                patternpage &= "<BR>" & vbCrLf
                                patternpage &= "<DIV ID=""mainplayer"" ALIGN=""center"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ STYLE=""background-color: black; border-radius: 8px; width: " & player_width.ToString & "px; height: " & player_height.ToString & "px; min-width: 160px; min-height: 120px;""></DIV>" & vbCrLf & vbCrLf

                                patternpage &= "<SCRIPT LANGUAGE=""javascript"">" & vbCrLf
                                patternpage &= " var so4 = new SWFObject('/player.swf','mpl','" & player_width.ToString & "','" & player_height.ToString & "','8');" & vbCrLf
                                patternpage &= " so4.addParam('allowscriptaccess','always');" & vbCrLf
                                patternpage &= " so4.addParam('allowfullscreen','true');" & vbCrLf
                                patternpage &= " so4.addVariable('width','" & player_width.ToString & player_prop & "');" & vbCrLf
                                patternpage &= " so4.addVariable('height','" & player_height.ToString & player_prop & "');" & vbCrLf
                                patternpage &= " so4.addVariable('file','" & GetHost() & "v/" & output_filename & "');" & vbCrLf
                                patternpage &= " so4.addVariable('searchbar','false');" & vbCrLf
                                patternpage &= " so4.addVariable('linkfromdisplay','true');" & vbCrLf & vbCrLf

                                patternpage &= " so4.write('mainplayer');" & vbCrLf
                                patternpage &= "</SCRIPT>" & vbCrLf
                                patternpage &= "<BR>" & vbCrLf & vbCrLf
                            Case "embed_flash"
                                'Flash via <embed>
                                patternpage &= "<!-- Embarcation directe du lecteur Flash -->" & vbCrLf & vbCrLf
                                patternpage &= "<EMBED ALIGN=CENTER SRC=""/player.swf"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ ID=""mainplayer"" allowfullscreen=""true"" allowscriptaccess=""always"" flashvars=""file=" & GetHost() & "v/" & output_filename & """ type=""application/x-shockwave-flash"" />" & vbCrLf '%26searchbar=false%26linkfromdisplay=true
                            Case "activex_flash"
                                'Flash via ActiveX
                                patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur Flash Player -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" CLASSID=""clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"" WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ CODEBASE=""" & GetHost() & "fp8axstp.exe"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""movie"" VALUE=""/player.swf"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""allowfullscreen"" VALUE=""true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""allowscriptaccess"" VALUE=""always"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""flashvars"" VALUE=""file=" & GetHost() & "v/" & output_filename & "%26searchbar=false%26linkfromdisplay=true"">" & vbCrLf
                                patternpage &= " <PARAM NAME=""wmode"" VALUE=""opaque"">" & vbCrLf
                                patternpage &= "</OBJECT>" & vbCrLf & vbCrLf
                            Case "object"
                                'Objet générique sans ActiveX
                                patternpage &= "<!-- Intégration d'un média de façon générique via Object -->" & vbCrLf & vbCrLf
                                patternpage &= "<OBJECT ALIGN=CENTER ID=""mainplayer"" DATA=""" & GetHost() & "v/" & output_filename & """ SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """></OBJECT>" & vbCrLf & vbCrLf
                            Case Else
                                'Si par mésaventure, le paramètre manque, affichage d'un lecteur générique.
                                patternpage &= "<!-- Fallback vers une intégration générique via la balise HTML embed -->" & vbCrLf & vbCrLf
                                patternpage &= "<EMBED ALIGN=CENTER ID=""mainplayer"" SRC=""" & GetHost() & "v/" & output_filename & """ MRL=""" & GetHost() & "v/" & output_filename & """ TARGET=""" & GetHost() & "v/" & output_filename & """ HREF=""" & GetHost() & "v/" & output_filename & """ FILENAME=""" & GetHost() & "v/" & output_filename & """ URL=""" & GetHost() & "v/" & output_filename & """ TYPE=""" & media_type & """ WIDTH=""" & player_width.ToString & player_prop & """ HEIGHT=""" & player_height.ToString & player_prop & """ autostart=""true"" />" & vbCrLf
                        End Select

                        If player_size = "fullscreen" Or player_size = "fulljs" Then
                            patternpage &= "</BODY></HTML>" & vbCrLf
                        Else
                            patternpage &= "</CENTER>" & vbCrLf
                            patternpage &= "<P><B>Publié le " & tmp_prop.DateOfRelease & " par " & tmp_prop.Creator & ". " & tmp_prop.Views & " vue(s).</B></P>" & vbCrLf
                            patternpage &= "<P STYLE=""text-align: justify;"">" & tmp_prop.Description & "</P><BR>" & vbCrLf
                            patternpage &= "  </TD>" & vbCrLf

                            If right_panel Then
                                patternpage &= "  <TD ROWSPAN=2 WIDTH=240>" & vbCrLf
                                patternpage &= "   <IFRAME BORDER=0 SRC=""" & GetHost() & "related?q=" & tmp_prop.Title.Replace(" ", "+") & "&exclude=" & watcharg & """ WIDTH=380 HEIGHT=1000 STYLE=""border: 0px;"" />Les iframes ne semblent pas disponibles sur votre navigateur actuel. Vous pouvez désactiver ce volet de suggestions dans les <A HREF=""/config.cgi"">paramètres</A>.</IFRAME>" & vbCrLf
                                patternpage &= "  </TD>" & vbCrLf
                                patternpage &= " </TR>" & vbCrLf
                            End If

                            patternpage &= "</TABLE></CENTER><BR><BR><BR>" & vbCrLf
                            patternpage &= footer & vbCrLf '"<DIV CLASS=""bodysep""></DIV><BR>" & 
                        End If

                        Dim watch_resp As String =
                            "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                            "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                            "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                            "Connection: close" & vbCrLf &
                            "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
                    Dim notfound_data As Byte() = GetHTTPBytes(500, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet, et ne peut donc pas traiter cette requête.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                    Try
                        stream.Write(notfound_data, 0, notfound_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try
                End If
            Else
                'Identifiant invalide manifestement!
                InitValues("Erreur de saisie", , wanted_skin, , used_player)
                patternpage &= " <P ALIGN=CENTER><BR><B>L'identifiant vidéo que vous avez entré semble invalide. Aucune lecture ne peut être poursuivie.<BR><BR>Cliquez <A HREF=""/"" STYLE=""color: " & link_color & ";"">ici</A> pour retourner à l'index.</B></P><BR><BR></BODY></HTML>" & vbCrLf & vbCrLf

                Dim watch_resp As String =
                    "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>Veuillez vous rendre sur ce <A HREF=""/"">lien</A> pour effectuer une recherche.</P>" & vbCrLf

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

            Dim result_page As String = "<H1>Erreur 400 - Requête erronée</H1><P>Vous devez préciser quel vidéo lire directement en flux, avec le paramètre <I>v</I>.<BR>Ex: http://" & last_host_2 & "/stream?v=BbCefdlDDTU<BR><BR>" & vbCrLf & "Click <A HREF=""/"">here</A> to go back to the index page.</P>" & vbCrLf

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
                    Dim psi As New ProcessStartInfo()
                    psi.FileName = "yt-dlp.exe"

                    Dim add_cookie As String = String.Empty

                    If IO.File.Exists("cookies.txt") Then
                        add_cookie &= " --cookies cookies.txt"
                        WriteLog("Usage du fichier cookies.txt ajouté par l'administrateur du serveur.", ConsoleColor.Magenta)
                    End If

                    If vt = RequestVideoType.LuckyVideo Then
                        psi.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<||>"" ""ytsearch1:" & req & """ --no-warnings --encoding utf-8 --user-agent ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/136.0 Safari/537.36""" & add_cookie
                    Else
                        psi.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<||>"" ""ytsearch" & number_of_results.ToString & ":" & req & """ --no-warnings --encoding utf-8 --user-agent ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/136.0 Safari/537.36""" & add_cookie
                    End If

                    psi.UseShellExecute = False
                    psi.RedirectStandardOutput = True
                    psi.RedirectStandardError = True
                    psi.CreateNoWindow = True
                    psi.StandardOutputEncoding = Encoding.UTF8
                    psi.StandardErrorEncoding = Encoding.UTF8

                    Dim p As Process = Process.Start(psi)
                    Dim output As String = p.StandardOutput.ReadToEnd() 'Récupération des résultats
                    Dim err As String = p.StandardError.ReadToEnd()

                    p.WaitForExit()
                    If vt <> RequestVideoType.LuckyVideo Then InitValues("Recherche de " & EscapeHtml(req), req, wanted_skin, , used_player)

                    patternpage &= "<HR WIDTH=880 ALIGN=CENTER /><BR>" & vbCrLf

                    'Récupération des lignes
                    If String.IsNullOrEmpty(output) Then
                        patternpage &= " <P ALIGN=CENTER><BR><B><FONT SIZE=4>Aucun résultat trouvé !</FONT></B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & vbCrLf & vbCrLf
                        WriteLog("La recherche du mot-clef '" & req & "' n'a donné aucun résultat.")
                    Else
                        output = output.Remove(output.Length - 4, 4)
                        Dim lines As String() = output.Split("<||>", StringSplitOptions.RemoveEmptyEntries)

                        If lines.Count = 0 Then
                            'S'il n'y a aucune ligne retournée.
                            If vt = RequestVideoType.LuckyVideo Then
                                Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Ceci est un message d'erreur générique pour annoncer qu'aucune vidéo avec le(s) mot-clef(s) spécifié(s) n'a été trouvée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

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
                                patternpage &= " <P ALIGN=CENTER><BR><B><FONT SIZE=4>Aucun résultat trouvé !</FONT></B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & vbCrLf & vbCrLf
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
                                    patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Le meilleur résultat pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;» :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                                Else
                                    patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Les " & lines.Count.ToString & " meilleurs résultats pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;» :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                                End If
                                patternpage &= "  <CENTER><TABLE BORDER=0 CELLPADDING=8 WIDTH=600 ALIGN=CENTER>" & vbCrLf

                                WriteLog("La recherche du mot-clef '" & req & "' a donné " & lines.Count.ToString & " résultat(s).")

                                For Each line In lines

                                    Dim parts As String() = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                                    For i As Integer = 0 To parts.Length - 1
                                        For j As Integer = 0 To &H1F
                                            parts(i) = parts(i).Replace(Chr(j), String.Empty)
                                        Next
                                    Next

                                    If parts.Length = 10 Then
                                        Dim id As String = parts(0)
                                        Dim title As String = parts(1)
                                        Dim tmp_prop As New VideoProperties
                                        title = CleanText(title)

                                        tmp_prop.Title = CleanText(parts(1))

                                        tmp_prop.Title = tmp_prop.Title.Replace(" ?", "&nbsp;?")
                                        tmp_prop.Title = tmp_prop.Title.Replace(" !", "&nbsp;!")
                                        tmp_prop.Title = tmp_prop.Title.Replace(" :", "&nbsp;:")
                                        tmp_prop.Title = tmp_prop.Title.Replace(" ;", "&nbsp;;")

                                        tmp_prop.Views = IIf(LCase(parts(2)) = "na", "0", GetThousands(parts(2)))
                                        tmp_prop.DateOfRelease = GetDate(parts(3))
                                        tmp_prop.Creator = CleanText(parts(4))
                                        tmp_prop.Duration = IIf(IsNumeric(parts(6)), GetDuration(parts(6)), "?:??")
                                        tmp_prop.Dimensions = IIf(IsNumeric(parts(7)), parts(7), "640") & ":" & IIf(IsNumeric(parts(8)), parts(8), "480")

                                        tmp_prop.Description = IIf(String.IsNullOrEmpty(parts(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(parts(9))))
                                        If tmp_prop.Description.Length > 1024 Then tmp_prop.Description = tmp_prop.Description.Substring(0, 1024)
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbCr, "<BR>")
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbLf, "<BR>")
                                        tmp_prop.Description = tmp_prop.Description.Replace(vbCrLf, "<BR>")
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
                                        patternpage &= "   <TR>" & vbCrLf
                                        patternpage &= "    <TD WIDTH=160 HEIGHT=100>" & vbCrLf 'BACKGROUND=""/thumbnail?t=" & id & """
                                        patternpage &= "     <A HREF=""/watch?v=" & id & """><IMG SRC=""/thumbnail?t=" & id & """ WIDTH=160 HEIGHT=100 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A>" & vbCrLf
                                        patternpage &= "    </TD>" & vbCrLf
                                        patternpage &= "    <TD WIDTH=* VALIGN=TOP>" & vbCrLf
                                        patternpage &= "     <A HREF=""/watch?v=" & id & """>" & EscapeHtml(title) & "</A> &bull; <A HREF=""/stream?v=" & id & """ STYLE=""color: " & link_color & ";"">Flux&nbsp;direct</A><BR>" & vbCrLf
                                        patternpage &= "     Vidéo publiée le " & tmp_prop.DateOfRelease & " par <I>" & EscapeHtml(tmp_prop.Creator) & "</I>.<BR>" & vbCrLf
                                        patternpage &= "     Durée: " & tmp_prop.Duration & " &bull; Vues: " & tmp_prop.Views & "<BR></TD>" & vbCrLf
                                        patternpage &= "   </TR>" & vbCrLf
                                    End If
                                Next

                                patternpage &= "  </TABLE></CENTER>"
                            End If
                        End If
                    End If

                    patternpage &= "<BR><BR>" & footer

                    'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                    Dim req_resp As String =
                        "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                        "Connection: close" & vbCrLf &
                        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
                        Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Message générique pour annoncer à l'utilisateur qu'aucun mot-clef n'a été spécifié.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

                        Try
                            stream.Write(notfound_data, 0, notfound_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                        End Try
                        client.Close()
                    Else
                        InitValues("Erreur de recherche", , wanted_skin, , used_player)
                        patternpage &= " <HR WIDTH=880 ALIGN=CENTER /><BR>" & vbCrLf
                        patternpage &= " <P ALIGN=CENTER><BR><B><FONT SIZE=2>Veuillez spécifier un mot-clef pour que la recherche puisse avoir lieu.<BR><BR>Cliquez <A HREF=""/"" STYLE=""color: " & link_color & ";"">ici</A> pour retourner à l'index.</FONT></B></P><BR><BR><DIV CLASS=""bodysep""></DIV>" & vbCrLf & vbCrLf & footer

                        Dim req_resp As String =
                            "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                            "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                            "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                            "Connection: close" & vbCrLf &
                            "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
                Dim notfound_data As Byte() = GetHTTPBytes(500, "<H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet, ainsi, la requête ne peut pas être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /related?q=") Then
            InitValues("Contenus relatifs", , , , , False)

            Dim get_params As String = String.Empty

            If IsNetworkAvailable() Then
                Dim arg As String = Split(request)(1)
                arg = arg.Remove(0, 8)

                'Les caractères systèmes sont retirés par sécurité
                For i As Integer = 0 To &H1F
                    request = request.Replace(Chr(i), String.Empty)
                Next

                Dim rel_params() As String = Nothing
                Dim req, excl As String
                req = String.Empty
                excl = String.Empty

                If arg.Contains("&") Then
                    rel_params = arg.Remove(0, 1).Split("&") 'Division des paramètres
                Else
                    req = arg.Remove(0, 2) 'Juste q=
                End If

                For Each r As String In rel_params
                    If r.StartsWith("q=") Then
                        req = r.Remove(0, 2)
                    ElseIf r.StartsWith("exclude=") Then
                        excl = r.Remove(0, 8)
                    End If
                Next

                'Récupérer les 10 vidéos en rapport avec le mot-clef spécifié
                req = UrlDecodeLatin1(req)
                req = req.Replace("+", " ")

                If Not String.IsNullOrEmpty(req) Then
                    If req.Contains("&") And Not req.EndsWith("&") Then
                        Dim req_f As Integer = req.IndexOf("&")
                        get_params = req.Remove(0, req_f)
                        req = req.Substring(0, req_f)
                    End If

                    WriteLog("Le mot-clef '" & req & "' a été demandé en mode relatif. Recherche en cours...", ConsoleColor.Yellow, client)

                    'Lancement de yt-dlp
                    Dim psi As New ProcessStartInfo()
                    psi.FileName = "yt-dlp.exe"

                    Dim add_cookie As String = String.Empty

                    If IO.File.Exists("cookies.txt") Then
                        add_cookie &= " --cookies cookies.txt"
                        WriteLog("Usage du fichier cookies.txt ajouté par l'administrateur du serveur.", ConsoleColor.Magenta)
                    End If

                    psi.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s<|>%(description)s<||>"" ""ytsearch" & number_of_results.ToString & ":" & req & """ --no-warnings --encoding utf-8 --user-agent ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/136.0 Safari/537.36""" & add_cookie

                    psi.UseShellExecute = False
                    psi.RedirectStandardOutput = True
                    psi.RedirectStandardError = True
                    psi.CreateNoWindow = True
                    psi.StandardOutputEncoding = Encoding.UTF8
                    psi.StandardErrorEncoding = Encoding.UTF8

                    Dim p As Process = Process.Start(psi)
                    Dim output As String = p.StandardOutput.ReadToEnd() 'Récupération des résultats
                    Dim err As String = p.StandardError.ReadToEnd()

                    p.WaitForExit()
                    InitValues("Contenus relatifs à " & EscapeHtml(req), req, wanted_skin, , used_player, False)

                    'Récupération des lignes

                    If String.IsNullOrEmpty(output) Then
                        patternpage &= " <P ALIGN=CENTER><B>Aucun contenu relatif trouvé !</B></P>" & vbCrLf
                    Else
                        output = output.Remove(output.Length - 4, 4)
                        Dim lines As String() = output.Split("<||>", StringSplitOptions.RemoveEmptyEntries)

                        If lines.Count = 0 Then
                            'S'il n'y a aucune ligne retournée.
                            patternpage &= " <P ALIGN=CENTER><B>Aucun contenu relatif trouvé !</B></P>" & vbCrLf
                        Else
                            'Sinon, on affiche les résultats dans la page Web.
                            patternpage &= "  <CENTER><TABLE BORDER=0 CELLPADDING=8 WIDTH=360 ALIGN=CENTER>" & vbCrLf

                            WriteLog("La recherche relative du mot-clef '" & req & "' a donné " & lines.Count.ToString & " résultat(s).")

                            For Each line In lines

                                Dim parts As String() = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                                For i As Integer = 0 To parts.Length - 1
                                    For j As Integer = 0 To &H1F
                                        parts(i) = parts(i).Replace(Chr(j), String.Empty)
                                    Next
                                Next

                                If parts.Length = 10 Then
                                    Dim id As String = parts(0)
                                    Dim title As String = parts(1)
                                    Dim tmp_prop As New VideoProperties
                                    title = CleanText(title)

                                    tmp_prop.Title = CleanText(parts(1))

                                    tmp_prop.Title = tmp_prop.Title.Replace(" ?", "&nbsp;?")
                                    tmp_prop.Title = tmp_prop.Title.Replace(" !", "&nbsp;!")
                                    tmp_prop.Title = tmp_prop.Title.Replace(" :", "&nbsp;:")
                                    tmp_prop.Title = tmp_prop.Title.Replace(" ;", "&nbsp;;")

                                    tmp_prop.Views = IIf(LCase(parts(2)) = "na", "0", GetThousands(parts(2)))
                                    tmp_prop.DateOfRelease = GetDate(parts(3))
                                    tmp_prop.Creator = CleanText(parts(4))
                                    tmp_prop.Duration = IIf(IsNumeric(parts(6)), GetDuration(parts(6)), "?:??")
                                    tmp_prop.Dimensions = IIf(IsNumeric(parts(7)), parts(7), "640") & ":" & IIf(IsNumeric(parts(8)), parts(8), "480")

                                    tmp_prop.Description = IIf(String.IsNullOrEmpty(parts(9)), "<I>Aucune description disponible.</I>", EscapeHtml(CleanText(parts(9))))
                                    If tmp_prop.Description.Length > 1024 Then tmp_prop.Description = tmp_prop.Description.Substring(0, 1024)
                                    tmp_prop.Description = tmp_prop.Description.Replace(vbCr, "<BR>")
                                    tmp_prop.Description = tmp_prop.Description.Replace(vbLf, "<BR>")
                                    tmp_prop.Description = tmp_prop.Description.Replace(vbCrLf, "<BR>")
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
                                    If String.IsNullOrEmpty(excl) OrElse parts(0) <> excl Then
                                        patternpage &= "   <TR>" & vbCrLf
                                        patternpage &= "    <TD WIDTH=120 HEIGHT=68>" & vbCrLf
                                        patternpage &= "     <A HREF=""/watch?v=" & id & """ TARGET=""_parent""><IMG SRC=""/thumbnail?t=" & id & """ WIDTH=120 HEIGHT=68 CLASS=""relatedthumb"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A>" & vbCrLf
                                        patternpage &= "    </TD>" & vbCrLf
                                        patternpage &= "    <TD WIDTH=* VALIGN=TOP>" & vbCrLf
                                        patternpage &= "     <A HREF=""/watch?v=" & id & """ TARGET=""_parent"">" & EscapeHtml(title) & "</A><BR>Durée: " & tmp_prop.Duration & " &bull; Vues: " & tmp_prop.Views.Replace(" ", "&nbsp;") & "<BR>Par <I>" & EscapeHtml(tmp_prop.Creator) & "</I>.</TD>" & vbCrLf
                                        patternpage &= "   </TR>" & vbCrLf
                                    End If
                                End If
                            Next

                            patternpage &= "  </TABLE></CENTER>" & vbCrLf
                        End If
                    End If

                    patternpage &= "<BR><BR>" & vbCrLf & "</BODY></HTML>"

                    'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                    Dim req_resp As String =
                        "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                        "Connection: close" & vbCrLf &
                        "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
                    InitValues("Erreur de recherche relative", , wanted_skin, , used_player, False)
                    patternpage &= " <P ALIGN=CENTER><BR><B>Veuillez spécifier un mot-clef pour que la recherche puisse avoir lieu.</B></P></BODY></HTML>" & vbCrLf

                    Dim req_resp As String =
                            "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                            "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                            "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                            "Connection: close" & vbCrLf &
                            "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

                    Dim req_data As Byte() = iso.GetBytes(req_resp)

                    Try
                        stream.Write(req_data, 0, req_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try
                End If

                client.Close()
            Else
                Dim notfound_data As Byte() = GetHTTPBytes(500, "<H1>Erreur 500 - Erreur interne du serveur</H1>" & vbCrLf & "<P>Le serveur proxy n'est pas connecté à Internet, ainsi, la requête ne peut pas être satisfaite.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /search") Or request.StartsWith("GET /related") Then
            'Requête vide
            Dim result_page As String = "<TITLE>RetroYT - Information</TITLE><H1>302 Ressource trouvée</H1><P>Veuillez vous rendre <A HREF=""/"">ici</A> pour chercher une vidéo.</P>" & vbCrLf

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
            'Requête vide
            'Index du site
            WriteLog("L'utilisateur demande le formulaire de recherche en mode chanceux.", , client)
            InitValues("Accueil", , wanted_skin, True, used_player)
            patternpage &= "<P ALIGN=CENTER><BR><B>Faire une recherche en mode chanceux renvoie une unique vidéo basée sur des mot-clefs à rechercher dans la zone ci-dessus.<BR><BR>Cliquez <A HREF=""/about.htm"" STYLE=""color: " & link_color & ";"">ICI</A> pour obtenir plus d'informations sur le fonctionnement.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & footer

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /thumbnail?t=") Then
            'Miniatures YouTube
            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 13)
            WriteLog("Miniature de la vidéo avec pour identifiant '" & arg & "' demandée... ", , client)
            Dim path As String = "thumbs\" & arg & ".jpg"

            'https://i.ytimg.com/vi/xxxxxxxxxxx/default.jpg

            If Not IO.File.Exists(path) Then
                Dim url As String = "https://i.ytimg.com/vi/" & arg & "/mqdefault.jpg"

                Try
                    Dim wc As New Net.WebClient()
                    wc.DownloadFile(url, path)
                    WriteLog("La miniature avec pour identifiant '" & arg & "' a été mise en cache.", ConsoleColor.Green)
                Catch ex As Exception
                    path = CurDir() & "\resfiles\nopic.jpg"
                    WriteLog("Erreur: Pas de miniature trouvée ! Envoi d'une miniature par défaut...", ConsoleColor.Red)
                End Try
            End If

            Dim bytes = IO.File.ReadAllBytes(path)

            Dim header As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: image/jpeg" & vbCrLf &
                "Connection: close" & vbCrLf &
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

            Dim selected_nopanel As String = String.Empty
            Dim selected_panel As String = String.Empty

            'Nombre de résultats par recherche et affichage en paramètres
            Select Case number_of_results
                Case 1 : selected_one = " SELECTED"
                Case 5 : selected_five = " SELECTED"
                Case 10 : selected_ten = " SELECTED"
                Case 20 : selected_twenty = " SELECTED"
                Case Else : selected_ten = " SELECTED"
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
                Case Else : selected_middle = " SELECTED"
            End Select

            'Codec vidéo/audio utilisé pour la lecture
            Select Case used_codec
                Case "mp4" : selected_mp4 = " SELECTED"
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
                Case "monochrome" : selected_monochrome = " SELECTED"
                Case Else : selected_cosmic = " SELECTED"
            End Select

            If right_panel Then
                selected_panel = " SELECTED"
            Else
                selected_nopanel = " SELECTED"
            End If

            InitValues("Configuration client", , wanted_skin, , used_player)
            patternpage &= "<BR><P ALIGN=CENTER><B><FONT SIZE=4>Configuration du client RetroYT :</FONT></B></P><BR>" & vbCrLf & vbCrLf

            If request.Contains("message=gotreset") Then
                patternpage &= "<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été remise par défaut avec succès (" & Now.ToString & ").</FONT></B></P></CENTER><BR>"
            ElseIf request.Contains("message=gotsaved") Then
                patternpage &= "<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été enregistrée avec succès (" & Now.ToString & ").</FONT></B></P></CENTER><BR>"
            End If

            patternpage &= "  <FORM METHOD=""POST"" ACTION=""/savecfg.cgi"">" & vbCrLf
            patternpage &= "   <CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=780>" & vbCrLf
            patternpage &= "    <TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Nombre de résultats affichés par recherche&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""results"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""1""" & selected_one & ">1 résultat</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""5""" & selected_five & ">5 résultats</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""10""" & selected_ten & ">10 résultats [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""20""" & selected_twenty & ">20 résultats</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Format vidéo et codec utilisés&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""codec"" WIDTH=300>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Formats Microsoft :</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_mpeg4""" & selected_avi_mpeg4 & ">AVI (MPEG-4, MP3) [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_msvideo1""" & selected_avi_msvideo1 & ">AVI (MSVideo1, PCM) [Windows 3.11/95/NT]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_cinepak""" & selected_avi_cinepak & ">AVI (Cinepak, PCM) [Lent à encoder]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""xvid""" & selected_xvid & ">AVI (Xvid, MP3)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_mjpeg""" & selected_avi_mjpeg & ">AVI (MJPEG, PCM)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_yuv""" & selected_avi_yuv & ">AVI (YUV, PCM) [Très lourd!]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""wmv1""" & selected_oldwmv & ">WMV (WMV1, WMAv1) [Windows 9x/NT]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""wmv2""" & selected_wmv & ">WMV (WMV2, WMAv2) [Windows 98/ME/2000]</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Formats Apple QuickTime :</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_cinepak""" & selected_mov_cinepak & ">MOV (Cinepak, PCM) [Lent] [MacOS 90s]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_mpeg4""" & selected_mov_mpeg4 & ">MOV (MPEG-4, MP2) [MacOS 90s]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_rpza""" & selected_mov_rpza & ">MOV (RPZA, PCM) [MacOS 90s]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_svq1""" & selected_mov_svq1 & ">MOV (Sorenson SVQ1, MP3) [MacOS X 2000s]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mov_mjpeg""" & selected_mov_mjpeg & ">MOV (MJPEG, PCM) [MacOS]</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Autres formats universels ou génériques :</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mp4""" & selected_mp4 & ">MP4 (H.264, AAC)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mpeg1""" & selected_mpg & ">MPEG (MPEG-1, MP2)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""rm""" & selected_rm & ">Real Media (RV10, AC3)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""3gp""" & selected_3gp & ">3GP (H.263, AMR-NB) [Mobile]</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Formats Flash Player :</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""flv""" & selected_flv & ">Macromedia Flash (Sorenson Spark, MP3)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Résolution de la vidéo&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""resolution"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""auto""" & selected_autosize & ">Automatique [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""96p""" & selected_96p & ">96p (Minimale)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""120p""" & selected_120p & ">120p (Très Faible)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""144p""" & selected_144p & ">144p (Faible)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""240p""" & selected_240p & ">240p (Basse)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""360p""" & selected_360p & ">360p (Moyenne)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""480p""" & selected_480p & ">480p (Standard)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""720p""" & selected_720p & ">720p (Haute) [HD]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""1080p""" & selected_1080p & ">1080p (Très Haute) [HD]</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Nombre d'images par seconde&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""framerate"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""auto""" & selected_framerate10 & ">Automatique [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""10""" & selected_framerate10 & ">10 images</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""12""" & selected_framerate12 & ">12 images</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""15""" & selected_framerate15 & ">15 images</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""20""" & selected_framerate20 & ">20 images</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""24""" & selected_framerate24 & ">24 images</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""25""" & selected_framerate25 & ">25 images</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""30""" & selected_framerate30 & ">30 images</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""60""" & selected_framerate60 & ">60 images (Déconseillé sur configurations anciennes)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Intégration multimédia utilisée&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""player"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""no_integration""" & selected_nointegration & ">(Aucune intégration)</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Lecteurs propriétaires et open source :</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""legacy_wmp""" & selected_legacy_wmp & ">Windows Media Player 6.4 (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""wmp""" & selected_wmp & ">Windows Media Player 7.0 et plus (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""quicktime""" & selected_quicktime & ">Lecteur Apple QuickTime (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""embed_quicktime""" & selected_embed_quick & ">Lecteur Apple QuickTime (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""vlc""" & selected_vlc & ">Lecteur VLC (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""alt_vlc""" & selected_altvlc & ">Lecteur VLC (Alternatif)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""embed_vlc""" & selected_vlcembed & ">Lecteur VLC (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""activex_realplayer""" & selected_realplayer_activex & ">Lecteur Real Player (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""realplayer""" & selected_realplayer & ">Lecteur Real Player (Embarqué)</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Intégration via Flash Player :</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""flash""" & selected_flashplayer & ">Lecteur Flash Player (Javascript)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""embed_flash""" & selected_embedflash & ">Lecteur Flash Player (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""activex_flash""" & selected_objectflash & ">Lecteur Flash Player (ActiveX)</OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED></OPTION>" & vbCrLf
            If (Not old_ie) Then patternpage &= "	   <OPTION DISABLED>Intégrations génériques et HTML5 :</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""embed""" & selected_embed & ">Intégration générique (Embarquée) [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""object""" & selected_genobject & ">Intégration générique (Object)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""video""" & selected_video & ">Intégration vidéo HTML5</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Taille du lecteur multimédia intégré&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""size"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""micro""" & selected_micro & ">Micro (160x140)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""ultrasmall""" & selected_ultrasmall & ">Ultra Compact (256x192)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""small""" & selected_small & ">Compact (320x240)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cs""" & selected_classic_size & ">Classique (480x360) [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""middle""" & selected_middle & ">Standard (640x480)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""large""" & selected_large & ">Large (854x480)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cinema""" & selected_cinema & ">Cinéma standard (1280x720)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""bigcinema""" & selected_big_cinema & ">Cinéma grand format (2560x1440)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""gold1""" & selected_gold1 & ">16:10 Standard (1280x800)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""gold2""" & selected_gold2 & ">16:10 Grand format (1440x900)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""auto""" & selected_auto & ">Automatique (Avec Javascript)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""autoheight""" & selected_auto_height & ">Automatique (Selon taille vidéo)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""fullscreen""" & selected_fullscreen & ">Plein écran (Avec HTML)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""fulljs""" & selected_fulljs & ">Plein écran (Avec Javascript)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Apparence de l'interface Web&nbsp;:&nbsp;</TD>" & vbCrLf & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""skin"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""oldyt""" & selected_classic & ">Classic</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cosmic""" & selected_cosmic & ">Cosmic Tube [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""modern""" & selected_modern & ">Modern</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""dark""" & selected_dark & ">Dark Mode</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""rose""" & selected_rose & ">Rose</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""aqua""" & selected_aqua & ">Aqua</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""monochrome""" & selected_monochrome & ">Monochrome</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf

            patternpage &= "    <TR>" & vbCrLf
            patternpage &= "     <TD ALIGN=RIGHT>Volet des suggestions&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "     <TD HEIGHT=40>" & vbCrLf
            patternpage &= "      <SELECT NAME=""panel"" WIDTH=300>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""true""" & selected_panel & ">Activé</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""false""" & selected_nopanel & ">Désactivé</OPTION>" & vbCrLf
            patternpage &= "      </SELECT>" & vbCrLf
            patternpage &= "     </TD>" & vbCrLf
            patternpage &= "    </TR>" & vbCrLf

            patternpage &= "   </TABLE></CENTER><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "   <CENTER><P>Cliquez sur le bouton pour <INPUT TYPE=""SUBMIT"" VALUE="" Enregistrer "" CLASS=""red_button"" /> ou sur le lien <A HREF=""/resetcfg.cgi"" STYLE=""color: " & link_color & ";"">réinitialiser les paramètres</A>.</P></CENTER>" & vbCrLf
            patternpage &= "  </FORM><BR>" & vbCrLf
            patternpage &= "  <NOSCRIPT><P ALIGN=CENTER><B>Javascript semble indisponible sur votre navigateur. Veuillez le réactiver ou changer de navigateur, si vous voulez utiliser certaines options.</B></P></NOSCRIPT><BR><BR>" & vbCrLf
            patternpage &= "  <VIDEO><P ALIGN=CENTER><B>Votre navigateur ne semble pas supporter le HTML5. Il est donc déconseillé d'utiliser<BR>l'intégration Video HTML5 pour lire du contenu multimédia.</B></P></VIDEO>"
            patternpage &= " <BR><BR>" & footer

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

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
                'client.Close()
                'rqcontent = "results=10&size=middle&codec=avi_mpeg4&player=embed&skin=cosmic&resolution=auto&framerate=auto"
                'Exit Sub

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
                "Set-Cookie: " & cookie_header & "results=10&size=cs&codec=avi_mpeg4&player=embed&skin=cosmic&resolution=auto&framerate=auto&panel=true; Path=/; Expires=" & exp & vbCrLf &
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
                notfound_data = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>La vidéo avec pour nom de fichier '" & arg1.Replace(">", "&gt;").Replace("<", "&lt;") & "' n'a pas été trouvée sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

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
                    Case "mp4" : media_type = "video/mp4"
                    Case "rm" : media_type = "application/vnd.rn-realmedia"
                    Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak", "xvid" : media_type = "video/x-msvideo"
                    Case "wmv1", "wmv2" : media_type = "video/x-ms-wmv"
                    Case "mov_cinepak", "mov_svq1", "mov_mpeg4", "mov_rpza" : media_type = "video/quicktime"
                    Case "mpeg1" : media_type = "video/mpeg"
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
                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 416 - Plage de données invalide</H1>" & vbCrLf & "<P>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour revenir à la page d'index.</P>" & vbCrLf)

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
        ElseIf request.StartsWith("GET /about.htm") Then
            'Afficher le "à propos" du proxy
            InitValues("À propos de RetroYT", , wanted_skin, , used_player)
            WriteLog("Page des informations sur le logiciel envoyée.", , client)

            patternpage &= "<BR><BR><CENTER><DIV STYLE=""display: block; width: 780px; margin-left: auto; margin-right: auto; text-align: left; text-align: justify;""><B>RetroYT</B> est un proxy multimédia pour YouTube développé en Visual Basic .NET 2022 par Monokeros. La version actuelle, la Bêta 5.5, a été publiée le 26 mai 2026. Ce projet est distribué gratuitement (sous la licence «&nbsp;freeware&nbsp;»), sans aucune garantie explicite ou implicite. L'auteur ne pourra être tenu responsable d'éventuels dommages matériels, logiciels, des éventuelles pertes de données, ou dysfonctionnements résultant de son utilisation, y compris dans un cadre normal.<BR>" & vbCrLf
            patternpage &= "Le projet vise principalement à restaurer la compatibilité de YouTube avec des systèmes d'exploitation, navigateurs web et lecteurs multimédia anciens ou obsolètes, à travers le relais de connexions, formatage vers un code HTML, et l'intégration de formats vidéo historiques, lisible par les navigateurs de toute époque." & vbCrLf
            patternpage &= "<BR><BR><BR>" & vbCrLf

            patternpage &= "<DIV STYLE=""display: block; border-radius: 8px; margin-left: 225px; border: 1px solid black; padding: 8px 8px 8px 8px; width: 40%;""><BR><CENTER><BIG><BIG><B>Sommaire: </B></BIG></BIG></CENTER><BR>" & vbCrLf
            patternpage &= "<A HREF=""#introduction"" STYLE=""color: " & link_color & ";"">I. Introduction</A><BR>" & vbCrLf
            patternpage &= "<A HREF=""#parameters"" STYLE=""color: " & link_color & ";"">II. Paramètres</A><BR>" & vbCrLf
            patternpage &= "<A HREF=""#precautions"" STYLE=""color: " & link_color & ";"">III. Précautions</A><BR>" & vbCrLf
            patternpage &= "<A HREF=""#configuration"" STYLE=""color: " & link_color & ";"">IV. Configuration</A><BR>" & vbCrLf
            patternpage &= "<A HREF=""#useget"" STYLE=""color: " & link_color & ";"">V. Utilisation du paramètre GET</A><BR>" & vbCrLf
            patternpage &= "<A HREF=""#credits"" STYLE=""color: " & link_color & ";"">VI. Remerciements</A><BR>&nbsp;</DIV><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<CENTER><H2><A NAME=""introduction"">I. Introduction</A></H2></CENTER><BR><BR>" & vbCrLf
            patternpage &= "Le nom «&nbsp;RetroYT&nbsp;» provient du terme «&nbsp;rétro&nbsp;», désignant de manière générale quelque chose d'ancien, de classique ou «&nbsp;à l'ancienne&nbsp;». Le logiciel repose sur un serveur Web codé directement dans l'application (dit «&nbsp;hardcodé&nbsp;»), servant d'intermédiaire entre YouTube et le navigateur client utilisé par l'utilisateur. L'objectif principal du projet est de restaurer un accès fonctionnel à YouTube sur des navigateurs et systèmes d'exploitation devenus trop anciens pour prendre en charge la version moderne du site. Bien que RetroYT puisse également être utilisé depuis un navigateur récent comme un proxy classique, pour fournir une version allégée du site, ce n'est pas sa vocation première. De nombreux proxies YouTube modernes existent déjà et offrent généralement de meilleures performances et une compatibilité plus étendue avec les standards Web actuels.<BR><B>RetroYT</B> vise avant tout à permettre la recherche et la lecture de vidéos YouTube depuis des environnements anciens ou obsolètes, tels que Windows 3.11, Windows 95, Windows 98, Windows NT 4.0, Windows 2000, certaines anciennes versions de MacOS, ainsi que divers systèmes UNIX/Linux historiques. La solution a également été testée sous Windows XP et Windows 11 avec succès. &Eacute;tant donné l'identité rétrocompatible de ce projet, il est donc parfaitement normal de retrouver, au sein de ce projet, du code HTML volontairement ancien, des méthodes d'intégration multimédia historiques, ou encore l'utilisation de technologies aujourd'hui abandonnées comme ActiveX, RealPlayer, des anciennes versions de QuickTime, Flash Player, ou les plugins NPAPI. L'ensemble du projet cherche à reproduire, autant que possible, une expérience cohérente avec les capacités techniques du Web des années 1990 et du début des années 2000, tout en offrant une expérience de navigation proche des services Internet actuels.<BR><BR>" & vbCrLf

            patternpage &= "<BR><CENTER><H2><A NAME=""parameters"">II. Paramètres</A></H2></CENTER><BR><BR>" & vbCrLf

            patternpage &= "<B>RetroYT</B> propose un ensemble de paramètres permettant d'adapter le fonctionnement du proxy aux capacités matérielles et logicielles du système cible. La méthode de récupération des fichiers depuis YouTube est agnostique. En d'autres termes, le proxy prendra le premier format venu, il peut être en extension MP4, WebM, ou MKV, etc. avec un encodage comme VP8, VP9, AV1, AV2, H.264, etc. Pour le fichier de destination, un second cache existe et permet de convertir dans un autre format compatible avec les anciennes configurations. Ces formats sont entièrement configurables depuis le navigateur client. L'utilisateur peut notamment choisir la taille du lecteur vidéo, le format et les codecs employés pour la conversion, ainsi que le nombre d'images par seconde. Pour les systèmes les plus anciens, comme Windows 95 ou Windows NT 4.0, l'utilisation des codecs MSVideo1 (Microsoft Video 1), MPEG-1 ou WMV1 est fortement recommandée, en raison de leur excellente compatibilité avec les anciennes versions de Windows. Comme beaucoup de codecs historiques, celui-ci produit toutefois des fichiers assez volumineux, en particulier pour les vidéos dépassant plusieurs minutes. Le codec Cinepak est aussi très rétrocompatible, mais peut mettre beaucoup de temps à être encodé. Pour les systèmes Apple, le format MOV avec les codecs RPZA ou Sorenson sont vivement conseillés. Les systèmes Linux étant très compatibles avec les formats PC, je conseille le format AVI MPEG-4 pour la lecture, et le format MP4 pour les systèmes plus récents, y compris ceux de Microsoft et Apple.<BR>Selon la puissance de votre machine, votre quantité de mémoire disponible ou la vitesse de votre connexion réseau, le transfert et la lecture des vidéos peuvent devenir plus difficiles. La résolution vidéo peut être choisie parmi un certain nombre de valeurs prédéfinies (96p, 120p, 144p, 240p, 360p, 480p, 720p et 1080p), ou laissée en mode automatique afin que le serveur sélectionne lui-même le format le plus approprié. Certains codecs anciens possèdent volontairement des limitations de résolution ou de format d'image, principalement pour des raisons de compatibilité avec les anciens lecteurs multimédia ou les contraintes matérielles des systèmes ciblés. Pareil pour le nombre d'images. Sélectionner le mode 60 images par seconde sur un format tel AVI Cinepak va immédiatement ramener à 30 images par seconde.<BR><BR>" & vbCrLf & vbCrLf
            patternpage &= "Le mode d'intégration du lecteur vidéo est également configurable. RetroYT peut utiliser différentes méthodes historiques de lecture multimédia, parmi lesquelles&nbsp;:" & vbCrLf & vbCrLf

            patternpage &= "<UL>" & vbCrLf
            patternpage &= " <LI>L'intégration ActiveX de Windows Media Player 6.4 ou supérieur ;</LI>" & vbCrLf
            patternpage &= " <LI>La balise HTML &lt;embed&gt; ;</LI>" & vbCrLf
            patternpage &= " <LI>L'intégration de lecteurs externes tels que VLC, QuickTime ou RealPlayer ;</LI>" & vbCrLf
            patternpage &= " <LI>Le fameux lecteur Flash Player, très utilisé aux grands débuts de YouTube ;</LI>" & vbCrLf
            patternpage &= " <LI>Ou encore la balise &lt;video&gt;, sous navigateurs modernes compatibles HTML5 (sortis après 2008).</LI>" & vbCrLf
            patternpage &= "</UL>" & vbCrLf

            patternpage &= "L'apparence générale de l'interface Web peut également être personnalisée grâce à plusieurs thèmes graphiques&nbsp;:" & vbCrLf & vbCrLf

            patternpage &= "<UL>" & vbCrLf
            patternpage &= " <LI><B>Classic :</B> Interface inspirée du site de YouTube des années 2000 ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Cosmic :</B> Reproduction fidèle du thème «&nbsp;Cosmic Panda&nbsp;» utilisé officiellement entre 2011 et 2013 sur ce même site ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Modern :</B> Interface proche du YouTube actuel ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Dark Mode :</B> Affichage clair sur fond sombre ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Rose :</B> Thème aux couleurs violacées, rappelant certaines interfaces Web des années 1990 ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Aqua :</B> Thème aux couleurs bleues, rappelant l'eau ;</LI>" & vbCrLf
            patternpage &= " <LI><B>Monochrome :</B> Thème aux couleurs monochromes, pour ceux qui ont des difficultés visuelles, ou qui préfèrent les interfaces sobres.</LI>"
            patternpage &= "</UL>" & vbCrLf & vbCrLf

            patternpage &= "Ces options permettent d'adapter RetroYT aussi bien à des machines très anciennes qu'à des systèmes plus récents, tout en conservant une esthétique cohérente avec les différentes époques du Web. Il est intéressant de noter qu'on peut lire aussi des flux vidéo depuis un lecteur externe, sans passer par l'interface Web. Il suffit pour cela de naviguer sur <I>http://adresse_serveur/stream?v=id_video</I> pour lire directement dans un lecteur externe comme VLC. Vous pouvez aussi chercher et lire la première vidéo trouvée de façon immédiate en naviguant sur <I>http://adresse_serveur/lucky?q=motclef</I>. Par défaut, le format permutera automatiquement sur MP4 si vous utilisez VLC. Notez bien que vous pouvez utiliser les paramètres GET documentés dans la <A HREF=""useget"">partie V</A> de cette documentation.<BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<BR><CENTER><H2><A NAME=""precautions"">III. Précautions</A></H2></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<B>RetroYT</B> est distribué sous licence freeware/open source et ne doit pas être revendu sans l'autorisation explicite de son auteur. Afin de conserver une compatibilité maximale avec les anciens navigateurs Web et systèmes d'exploitation, le proxy ne met volontairement pas en œuvre certaines technologies modernes de sécurisation des communications, notamment SSL/TLS côté client. Les échanges entre RetroYT et YouTube utilisent bien des connexions sécurisées modernes, mais les communications entre le client et le proxy restent, quant à elles, entièrement non chiffrées. En effet, nombre d'anciens navigateurs ne prennent pas en charge SSL/TLS, surtout dans leurs dernières versions. Le HTTP sans chiffrement est une solution universelle pour se connecter au serveur.<BR>Pour cette raison, RetroYT est principalement destiné à une utilisation au sein d'un réseau local (LAN), sur une machine personnelle ou dans un environnement contrôlé. Il est fortement déconseillé d'exposer directement le proxy sur Internet ou de l'utiliser sur un réseau public non sécurisé, sauf si vous utilisez des solutions complémentaires de protection telles qu'un VPN ou un tunnel sécurisé.<BR><BR>"
            patternpage &= "RetroYT utilise également un système de cache local afin d'améliorer les performances et limiter les téléchargements répétés. Quatre dossiers principaux sont utilisés&nbsp;:" & vbCrLf & vbCrLf
            patternpage &= "<UL>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>thumbs</I> : Stockage des miniatures YouTube (Qualité moyenne, alias MQ) envoyées au client à la demande ;</LI>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>srccache</I> : Stockage des vidéos sources et mises en cache pour être converties ;</LI>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>vidcache</I> : Stockage des vidéos converties et mises en cache pour être envoyées au client ;</LI>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>prclocks</I> : Stockage des fichiers qui permet de mémoriser les téléchargements ou conversions en cours d'exécution.</LI>" & vbCrLf
            patternpage &= "</UL>" & vbCrLf & vbCrLf

            patternpage &= "Ces dossiers peuvent être vidés manuellement si l'espace disque disponible devient insuffisant. Normalement, le logiciel gère lui-même la taille du cache et/ou le nombre de fichiers. Le dossier <I>srvlogs</I> contient tous les fichiers de rapport de connexion et des actions du serveur, avec heure et date. Bien que ces fichiers soient facultatifs et aisément supprimables, en revanche, certains fichiers et répertoires sont indispensables au fonctionnement du logiciel et ne doivent pas être supprimés&nbsp;:" & vbCrLf & vbCrLf

            patternpage &= "<UL>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>resfiles</I>, qui contient les ressources du projet, comme les images du site Web interne ;</LI>" & vbCrLf
            patternpage &= " <LI>Le dossier <I>flplayer</I>, qui contient les fichiers du lecteur Flash Player, au cas où il serait activé ;</LI>" & vbCrLf
            patternpage &= " <LI>Les fichiers <I>YTSrv.deps.json</I>, et <I>YTSrv.runtimeconfig.json</I> qui sont des scripts json vitaux pour que les binaires fonctionnent ;</LI>" & vbCrLf
            patternpage &= " <LI>Les fichiers <I>YTSrv.dll</I> et <I>YTSrv.pdb</I>, générés par Visual Basic .NET et indispensables au fonctionnement du logiciel ;</LI>" & vbCrLf
            patternpage &= " <LI><I>ffmpeg.exe</I> mis par les soins de l'utilisateur dans le dossier du proxy. Il s'agit d'un programme crucial qui permet de convertir à la volée les fichiers vidéo téléchargés vers un format compatible avec les anciennes configurations ;</LI>" & vbCrLf
            patternpage &= " <LI><I>yt-dlp.exe</I> mis par les soins de l'utilisateur, également dans le dossier du proxy. Il permet d'obtenir des vidéos depuis YouTube ;</LI>" & vbCrLf
            patternpage &= " <LI><I>RetroYT.exe</I> qui est le fichier binaire de lancement du logiciel lui-même.</LI>" & vbCrLf
            patternpage &= "</UL>" & vbCrLf & vbCrLf

            patternpage &= "La suppression de ces éléments empêcherait le démarrage ou le fonctionnement correct du proxy. Si le serveur est fermé pendant la conversion d'un ou plusieurs fichiers vidéo, sachez que des fichiers temporaires nommés <I>output_xxxx.lock</I> (où xxxx est un hash MD5 unique) sont générés avant le début de la conversion. Au cas où vous redémarreriez le logiciel, ces fichiers contiennent le(s) identifiant(s) des processus de ffmpeg.exe dernièrement lancés, ainsi que les fichiers qui étaient en cours de traitement. Ainsi, les processus fantômes de ffmpeg seront coupés, les fichiers temporaires seront supprimés, ainsi que les fichiers vidéo dont les conversions ont été inaccomplies, pour éviter tout fichier corrompu et tout plantage. Idem pour les fichiers en cours de téléchargement avec <I>download_xxxxxx.lock</I> où xxxxxx est un hash MD5 unique.<BR><BR>" & vbCrLf & vbCrLf
            patternpage &= "Si, côté client, les recherches n'affichent aucun résultat quel que soit le mot-clé renseigné, cela peut venir du fait que yt-dlp n'est pas reconnu par YouTube comme un navigateur web classique, mais comme un trafic automatisé (un «&nbsp;bot&nbsp;»). Dans ce cas, YouTube peut limiter ou bloquer les requêtes de recherche effectuées de façon anonyme. Pour contourner ce problème, vous pouvez ajouter un fichier ""cookies.txt"" dans le dossier de RetroYT. Celui-ci permet à YT-DLP d'utiliser une session YouTube existante afin d'effectuer les recherches comme si elles provenaient d'un utilisateur déjà connecté, plutôt que d'une session anonyme pouvant être plus limitée. Le fichier cookies.txt peut être exporté depuis votre navigateur web (Firefox, Chrome, Edge, etc.) à l'aide d'une extension dédiée comme «&nbsp;Get cookies.txt LOCALLY&nbsp;». Il ne s'agit pas simplement de copier les fichiers internes du profil Firefox, car leur format n'est pas directement exploitable par YT-DLP. <B>Attention toutefois:</B> si vous partagez ce proxy avec d'autres utilisateurs, ceux-ci n'auront pas accès à votre compte Google ni à vos données personnelles directement, mais les résultats de recherche pourront être influencés par l'activité de votre compte YouTube (historique, préférences, recommandations, personnalisation, etc.). En d'autres termes, les résultats affichés risquent d'être partiellement biaisés par votre propre utilisation préalable de YouTube, et avoir des vidéos adaptées à votre propre activité.<BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<BR><CENTER><H2><A NAME=""configuration"">IV. Configuration</A></H2></CENTER><BR><BR>" & vbCrLf
            patternpage &= "Du côté du serveur, il est recommandé d'exécuter RetroYT sur une machine relativement performante. Une connexion Internet stable et rapide est également recommandé. Le transcodage vidéo effectué par FFmpeg peut solliciter fortement le processeur, en particulier lors de l'utilisation de codecs anciens ou peu optimisés comme Cinepak ou MSVideo1. Windows 10 et Windows 11 sont actuellement les systèmes les plus recommandés pour héberger le proxy. Le logiciel nécessite l'environnement .NET 6.0 ou plus, afin de fonctionner correctement. Du côté client, RetroYT a été conçu pour rester accessible à des navigateurs et systèmes beaucoup plus anciens. La navigation sur le proxy ainsi que la lecture vidéo intégrée ont notamment été testées avec succès sur les configurations suivantes&nbsp;:<BR><BR>" & vbCrLf & vbCrLf
            patternpage &= "<UL>" & vbCrLf
            patternpage &= " <LI>Windows NT 4.0 SP6, Internet Explorer 5.5, Windows Media Player 6.4, 1Go de RAM, 32Mo de mém. vidéo et proc. de 700MHz ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows 2000 SP4, Internet Explorer 6.0, Windows Media Player 9.0, 3Go de RAM, 256Mo de mém. vidéo et proc. de 1,85GHz ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows XP, Internet Explorer 6.0, Windows Media Player 11.0, 2Go de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows XP, Mozilla Firefox 52.0, Plugin de VLC Media Player 3.0, 2Go de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows ME, Internet Explorer 5.5, Windows Media Player 7.0, 1Go de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows 98 SE, Internet Explorer 4.01, Flash Player 8, 1Go de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows 95 OSR 2.0, Internet Explorer 3.0, ActiveMovie et Media Player, 128Mo de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows 3.11, Internet Explorer 4.01, Real Player 5.0, 64Mo de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows NT 3.51, Internet Explorer 4.01, Real Player 5.0, 64Mo de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>MacOS X 7.5.3, NetScape 1.1 et Internet Explorer 4.01, Apple QuickTime 3, 512Mo de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Linux CentOS 6.10, SeaMonkey 2.49.7, Totem et GStreamer, 2Go de RAM ;</LI>" & vbCrLf
            patternpage &= " <LI>Windows 11, Opera 130.0, Intégration vidéo HTML5 avec 16Go de RAM, 2,8GHz de processeur, et 6Go de mémoire vidéo.</LI>" & vbCrLf
            patternpage &= "</UL><BR>" & vbCrLf & vbCrLf

            patternpage &= "Veillez à autoriser l'exécution des contrôles ActiveX, si vous utilisez un système d'exploitation de Microsoft. Veillez aussi à avoir un ou plusieurs lecteurs multimédias installés, et les cookies activés sur votre navigateur. En effet, RetroYT fait usage d'un cookie pour mémoriser les paramètres du client. Si ce dernier ne semble pas prendre en charge les cookies, vous pourrez toujours faire usage des paramètres GET dans l'URL de /watch, /lucky, ou /stream. Pour les très anciennes versions de Windows, faire usage du codec AVI MSVideo1 depuis la section ""Paramètres"" est recommandé, en résolution 240p et en 15 images/s, tout en veillant à ce que les vidéos ne dépassent pas 10 minutes de longueur. Il s'agit d'un codec avec compression intégrée, totalement compatible avec Windows depuis sa version 3.1. Pour les navigateurs compatibles HTML5, vous pouvez activer l'utilisation du format vidéo MP4, et l'intégration multimédia via la balise &lt;video&gt;.<BR>" & vbCrLf
            patternpage &= "Si vous activez le lecteur Flash Player, seul le format FLV (Flash Video) pourra être lu. Pareil pour Real Player, seul le format Real Media sera lu. Si par malheur aucune de ces options ne fonctionne, vous pouvez également cliquer sur le lien pour lire le flux vidéo directement (lien présent sous le lecteur, si présent). Le navigateur ouvrira un lecteur externe, ou vous proposera de télécharger le fichier pour le lire après. Mais il s'agit d'une option de dernier recours. Concernant le lecteur Windows Media Player, notez bien que l'utilisation des URL n'est prise en charge qu'à partir de la version 6.4.<BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<BR><CENTER><H2><A NAME=""useget"">V. Utilisation du paramètre GET</A></H2></CENTER><BR><BR>" & vbCrLf

            patternpage &= "Si les cookies ne fonctionnent pas sur votre navigateur, et que vous ne pouvez pas enregistrer les paramètres, ceux par défaut seront appliqués. Par conséquent, certaines fonctionnalités seront incompatibles avec votre configuration. Heureusement, RetroYT inclut une fonctionnalité pour remédier à cet éventuel manque. Pour modifier la configuration de la lecture sans passer par les cookies (et la sauvegarde du paramétrage qui utilise une requête POST), vous pouvez ajouter des paramètres GET dans l'URL qui suit le modèle <I>/watch?v=xxxxxxxxxxx</I>. Ce sont les mêmes attributs que ceux utilisés dans la requête POST ou dans le cookie lui-même. Vous pouvez changer le type de lecteur utilisé, la taille du lecteur, le format vidéo utilisé, le nombre d'images par seconde et la résolution.<BR><BR><BR>" & vbCrLf

            patternpage &= "<CENTER><B>Le lecteur utilisé se change via l'entête <I>player</I> avec pour paramètre un des éléments suivants&nbsp;:</B></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<TABLE BORDER=1 CELLPADDING=4 ALIGN=CENTER>" & vbCrLf
            patternpage &= " <TR><TD>no_integration</TD><TD>Aucune intégration</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>legacy_wmp</TD><TD>Lecteur Windows Media Player 6.4</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>wmp</TD><TD>Lecteur Windows Media Player 7.0</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>embed</TD><TD>Intégration par la balise HTML &lt;embed&gt;</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>video</TD><TD>Intégration avec la balise &lt;video&gt; de HTML5</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>realplayer</TD><TD>Lecteur Real Player via &lt;embed&gt;</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>activex_realplayer</TD><TD>Lecteur Real Player via ActiveX</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>embed_vlc</TD><TD>Lecteur VLC via &lt;embed&gt;</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>vlc</TD><TD>Lecteur VLC via ActiveX</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>alt_vlc</TD><TD>Lecteur VLC via ActiveX (Avec un CLSID alternatif)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>quicktime</TD><TD>Lecteur QuickTime via ActiveX</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>embed_quicktime</TD><TD>Lecteur QuickTime via &lt;embed&gt;</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>flash</TD><TD>Lecteur Flash via Javascript</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>embed_flash</TD><TD>Lecteur Flash via &lt;embed&gt;</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>activex_flash</TD><TD>Lecteur Flash via ActiveX</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>object</TD><TD>Intégration via la balise HTML object</TD></TR>" & vbCrLf
            patternpage &= "</TABLE><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<CENTER><B>La taille du lecteur utilisée se règle avec l'entête <I>size</I>, suivi par un des paramètres suivants&nbsp;:</B></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<TABLE BORDER=1 CELLPADDING=4 ALIGN=CENTER>" & vbCrLf
            patternpage &= " <TR><TD>auto</TD><TD>Taille gérée par Javascript (Il doit être disponible et activé)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>micro</TD><TD>Taille 160x120 pixels (Pour les écrans de portables)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>ultrasmall</TD><TD>Taille 256x192 pixels</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>small</TD><TD>Taille 320x240 pixels (Pour les écrans VGA de base)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>middle</TD><TD>Taille 640x480 pixels (Taille par défaut)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>large</TD><TD>Taille 854x480 pixels (Format large minimal)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>cinema</TD><TD>Taille 1280x720 pixels (Format large standard)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>autoheight</TD><TD>Taille du lecteur basée sur la taille de la vidéo</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>fullscreen</TD><TD>Taille du lecteur sur toute la fenêtre (avec HTML)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>fulljs</TD><TD>Taille du lecteur sur toute la zone visible (avec Javascript)</TD></TR>" & vbCrLf
            patternpage &= "</TABLE><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<CENTER><B>Le format vidéo utilisé pour la lecture a pour entête <I>codec</I>, et est accompagné d'un des paramètres suivants&nbsp;:</B></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<TABLE BORDER=1 CELLPADDING=4 ALIGN=CENTER>" & vbCrLf
            patternpage &= " <TR><TD>mpeg1</TD><TD>Format MPEG, codec vidéo MPEG-1, codec audio MP2</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>avi_mpeg4</TD><TD>Format AVI (Microsoft), codec vidéo MPEG-4, codec audio MP3</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>avi_msvideo1</TD><TD>Format AVI (Microsoft), codec vidéo MSVideo1, codec audio PCM</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>avi_cinepak</TD><TD>Format AVI (Microsoft), codec vidéo Cinepak, codec audio PCM</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>avi_yuv</TD><TD>Format AVI (Microsoft), vidéo en YUV, codec audio PCM</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>mp4</TD><TD>Format MP4, codec vidéo H.264, codec audio AAC</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>wmv1</TD><TD>Format WMV, codec vidéo WMV1, codec audio WMAv1</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>wmv2</TD><TD>Format WMV, codec vidéo WMV2, codec audio WMAv2</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>mov_cinepak</TD><TD>Format Apple QuickTime (MOV), codec vidéo Cinepak, codec audio PCM</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>mov_svq1</TD><TD>Format Apple QuickTime (MOV), codec vidéo Sorenson SVQ1, codec audio MP3</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>mov_mpeg4</TD><TD>Format Apple QuickTime (MOV), codec vidéo MPEG-4, codec audio MP3</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>mov_rpza</TD><TD>Format Apple QuickTime (MOV), codec vidéo RPZA, codec audio PCM</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>3gp</TD><TD>Format 3GP (3G Video), codec vidéo H.263, codec audio AMR Narrowband</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>flv</TD><TD>Format Flash Video, codec vidéo Sorenson Spark, codec audio MP3</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>rm</TD><TD>Format Real Media, codec vidéo RV10, codec audio AC3</TD></TR>" & vbCrLf
            patternpage &= "</TABLE><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<CENTER><B>Le nombre d'images est paramétré avec l'entête <I>framerate</I> suivi du nombre d'images voulues UNIQUEMENT parmi cette liste&nbsp;:</B></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<TABLE BORDER=1 CELLPADDING=4 ALIGN=CENTER>" & vbCrLf
            patternpage &= " <TR><TD>auto</TD><TD>Meilleur nombre d'images par seconde pour le format vidéo voulu.</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>10</TD><TD>10 images par seconde (Pour vieux ordinateurs)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>12</TD><TD>12 images par seconde</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>15</TD><TD>15 images par seconde (Bon rapport qualité/quantité pour les vieux ordinateurs)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>20</TD><TD>20 images par seconde</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>24</TD><TD>24 images par seconde [Par défaut]</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>25</TD><TD>25 images par seconde</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>30</TD><TD>30 images par seconde</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>60</TD><TD>60 images par seconde (Vivement déconseillé sur les anciens PC)</TD></TR>" & vbCrLf
            patternpage &= "</TABLE><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<CENTER><B>La résolution de la vidéo, intitulée <I>resolution</I>, peut être choisie parmi les paramètres suivants&nbsp;:</B></CENTER><BR><BR>" & vbCrLf
            patternpage &= "<TABLE BORDER=1 CELLPADDING=4 ALIGN=CENTER>" & vbCrLf
            patternpage &= " <TR><TD>auto</TD><TD>Meilleure résolution choisie par le serveur, pour chaque format voulu</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>96p</TD><TD>Résolution minimale, surtout utile pour le format 3GP</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>120p</TD><TD>Résolution très faible</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>144p</TD><TD>Résolution faible</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>240p</TD><TD>Petite résolution (Recommandée pour toutes les configurations anciennes)</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>360p</TD><TD>Moyenne résolution</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>480p</TD><TD>Résolution standard</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>720p</TD><TD>Haute résolution [HD]</TD></TR>" & vbCrLf
            patternpage &= " <TR><TD>1080p</TD><TD>Très haute résolution [HD] (Pour les PC de la génération de Windows Vista et plus)</TD></TR>" & vbCrLf
            patternpage &= "</TABLE><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "Sacrées listes, n'est-ce pas? Certaines résolutions seront indisponibles sous certains formats. Pareil pour le nombre d'images. Ceci pour des raisons de limitations techniques fixées par les créateurs du codec, ou pour éviter toute saturation de la mémoire. Les paramètres erronés ou inexistants seront ignorés. Pour illustrer un usage concret de cette fonctionnalité&nbsp;:<BR><BR>" & vbCrLf & vbCrLf
            patternpage &= "<I>http://127.0.0.1/watch?v=dQw4w9WgXcQ&player=video&size=auto&codec=mp4&framerate=24&resolution=480p</I><BR><BR>" & vbCrLf
            patternpage &= "Visiter cette URL démarrera la lecture de la vidéo indiquée au format MP4 (Résolution 480p @ 24 FPS), via le lecteur vidéo intégré de HTML5. La taille du lecteur sera automatiquement réglée. Cette configuration par défaut est très utile pour les navigateurs prenant en charge le HTML5.<BR><BR>" & vbCrLf
            patternpage &= "Tous les paramètres ne sont pas obligatoires. Ainsi, pour démarrer, par exemple, une lecture avec le lecteur Flash :<BR><BR>" & vbCrLf
            patternpage &= "<I>http://127.0.0.1/watch?v=ZyhrYis509A&player=flash&codec=flv&resolution=240p</I><BR><BR>" & vbCrLf
            patternpage &= "Le tout en 240p, avec le nombre d'images par seconde par défaut. Le reste des paramètres utiliseront ceux par défaut également. Cette configuration reste assez typique de l'époque de Flash Player, dans les années 2000.<BR>" & vbCrLf
            patternpage &= "Si vous lisez depuis Windows 3.11 ou Windows NT 3.51, je vous conseille d'installer Real Player 5.0, qui rendra possible la lecture sous Internet Explorer 4 ou 5 via intégration ou ActiveX. Les paramètres à utiliser seront ainsi :<BR><BR>" & vbCrLf
            patternpage &= "<I>http://127.0.0.1/watch?v=FuOhQZP821o&player=realplayer&codec=rm&resolution=240p&framerate=15</I><BR><BR>" & vbCrLf
            patternpage &= "Ce ne sont que des exemples, mais ils vous inspireront probablement pour votre configuration. Faites-en bon usage.<BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "<BR><CENTER><H2><A NAME=""credits"">VI. Remerciements</A></H2></CENTER><BR><BR>" & vbCrLf
            patternpage &= "YouTube est une propriété de Google. Il s'agit d'une plateforme de diffusion de vidéos en direct, ou en différé. Ce projet de proxy n'est pas affilié à Google, ni à YouTube." & vbCrLf
            patternpage &= "Ce logiciel a été développé sous Microsoft Visual Basic .NET 2022. Il fait usage des librairies et binaires ffmpeg, et du projet yt-dlp, que l'utilisateur doit intégrer manuellement au dossier (ils ne sont pas livrés par défaut pour éviter des conflits d'intérêt avec leurs auteurs respectifs, et pour des raisons d'espace utilisé). En revanche, SWFObject est inclus au projet directement, car sous licence MIT. Il est donc libre de le redistribuer, et permet la lecture des vidéos au format Macromedia Flash lorsque l'utilisateur active cette fonctionnalité. Merci à ceux qui l'ont programmé.<BR>Merci aussi à ChatGPT pour ses astuces de programmation. Sans lui, ce projet n'aurait peut-être jamais vu le jour. Je remercie également LeJarb pour le code d'intégration de Real Player, et son optimisation de l'usage des codecs (en s'aidant de Léo AI). Je le remercie aussi pour ses divers feedbacks, et sa participation active dans l'amélioration du projet. Je remercie aussi Val pour ses tests du logiciel sur des configurations réelles. Merci également à vous, l'utilisateur, pour avoir utilisé RetroYT, en espérant qu'il fonctionnera parfaitement sur votre configuration, et qu'il vous procurera entière satisfaction dans l'usage du service YouTube depuis d'anciens systèmes. Voici la page de débug du projet: <A HREF=""/debug.cgi"" STYLE=""color: " & link_color & ";"">Cliquez ici</A>.<BR><BR><I>L'auteur.</I><BR><BR>" & vbCrLf & vbCrLf
            patternpage &= "<A HREF=""/"" STYLE=""color: " & link_color & ";"">Cliquez ici pour retourner à l'index</A><BR><BR>" & vbCrLf
            patternpage &= "</DIV></CENTER><DIV CLASS=bodysep></DIV>" & footer

            Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
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

            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 1)

            Dim fs As System.IO.FileStream = Nothing
            Dim resBuffer(8191) As Byte
            Dim resread As Integer = 0

            If arg.Length = 0 Then
                'Index du site
                WriteLog("L'utilisateur demande l'index du site. Renvoi vers la page d'accueil.", , client)
                InitValues("Accueil", , wanted_skin, , used_player)
                patternpage &= "<HR WIDTH=880 ALIGN=CENTER /><BR>" & vbCrLf
                patternpage &= "<P ALIGN=CENTER><BR><B>Pour commencer, veuillez entrer un mot-clef à rechercher dans la zone ci-dessus.<BR><BR>Cliquez <A HREF=""/about.htm"" STYLE=""color: " & link_color & ";"">ICI</A> pour obtenir plus d'informations sur le fonctionnement.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & footer

                Dim index_resp As String =
                "HTTP/" & http_ver & " 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & patternpage

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            Else
                'Ressource hardcodée ou hébergée
                'WriteLog("Fichier demandé par le client: " & arg, , client)

                Dim sent_res As String = "HTTP/" & http_ver & " 200 OK" & vbCrLf
                Dim sent_data As Byte()

                If arg.Contains("?") Then
                    arg = arg.Substring(0, arg.IndexOf("?"))
                End If

                If arg.Contains("/..") Then arg = arg.Replace("/..", String.Empty)
                If arg.Contains("../") Then arg = arg.Replace("../", String.Empty)
                If arg.Contains("/.") Then arg = arg.Replace("/.", String.Empty)
                If arg.Contains("./") Then arg = arg.Replace("./", String.Empty)

                If arg.Contains("\..") Then arg = arg.Replace("\..", String.Empty)
                If arg.Contains("..\") Then arg = arg.Replace("..\", String.Empty)
                If arg.Contains("\.") Then arg = arg.Replace("\.", String.Empty)
                If arg.Contains(".\") Then arg = arg.Replace(".\", String.Empty)

                Select Case LCase(arg)
                    Case "yt_logo2.gif", "yt_logo.gif", "yt_modrn.gif", "yt_dark.gif", "yt_rose.gif", "yt_aqua.gif", "yt_mono.gif", "cosmic.gif"
                        'Les logos RetroYT, qui font penser à ceux de YouTube, sont mis au format GIF pour garantir une compatibilité maximale avec les navigateurs anciens.
                        'Aussi cosmic.gif.

                        Try
                            sent_res &= "Content-Type: image/gif" & vbCrLf
                            sent_res &= "Connection: close" & vbCrLf
                            sent_res &= "Accept-Ranges: bytes" & vbCrLf
                            sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                            sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg).ToString & vbCrLf & vbCrLf
                            sent_data = iso.GetBytes(sent_res)

                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg, IO.FileMode.Open, IO.FileAccess.Read)

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
                    Case "btn_grad.png", "hot_grad.png", "btn_pink.png", "hot_pink.png", "hot_aqua.png", "btn_aqua.png"
                        sent_res &= "Content-Type: image/png" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg).ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg, IO.FileMode.Open, IO.FileAccess.Read)

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
                    Case "fp8axstp.exe"
                        'Envoi du plugin ActiveX pour QuickTime au format OCX.
                        sent_res &= "Content-Type: application/vnd.ms-cab-compressed" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\fp8axstp.exe").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\fp8axstp.exe", IO.FileMode.Open, IO.FileAccess.Read)

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
                    Case "qtplugin.cab"
                        'Envoi du plugin ActiveX pour QuickTime au format OCX.
                        sent_res &= "Content-Type: application/vnd.ms-cab-compressed" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Cache-Control: max-age=86400" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\qtplugin.cab").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)

                        Try
                            stream.Write(sent_data, 0, sent_data.Length)
                        Catch ex As Exception
                            WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                            client.Close()
                            Exit Sub
                        End Try

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\qtplugin.cab", IO.FileMode.Open, IO.FileAccess.Read)

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
                            Case Else
                                sent_css &= " background-color: #ffffff;" & vbCrLf
                                sent_css &= " color: #000000;" & vbCrLf
                        End Select

                        If wanted_skin = "cosmic" Then sent_css &= " background-image: url('cosmic.gif');" & vbCrLf

                        sent_css &= " font-family: Tahoma, Roboto, Arial, sans-serif;" & vbCrLf
                        sent_css &= " padding: 12px 12px 12px 12px;" & vbCrLf
                        If Not LCase(ua_string).Contains("msie 3.") Then sent_css &= " line-height: 18px;" 'Pour éviter des décalages bizarres des pages Web sous IE 3.0 " /* **/line-height: 18px; /** */"
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
                        sent_css &= " width: 160px;" & vbCrLf
                        sent_css &= " height: 100px;" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".relatedthumb {" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " width: 120px;" & vbCrLf
                        sent_css &= " height: 68px;" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "iframe {" & vbCrLf
                        sent_css &= " border: 0;" & vbCrLf
                        sent_css &= " width: 380px;" & vbCrLf
                        sent_css &= " min-height: 1000px;" & vbCrLf
                        sent_css &= " !height: 1000px;" & vbCrLf
                        sent_css &= " border-radius: 8px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a {" & vbCrLf

                        If wanted_skin = "dark" Then
                            sent_css &= " color: white;" & vbCrLf
                        Else
                            sent_css &= " color: black;" & vbCrLf
                        End If

                        sent_css &= " font-weight: bold;" & vbCrLf
                        If wanted_skin <> "monochrome" Then sent_css &= " text-decoration: none;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a:hover {" & vbCrLf
                        sent_css &= " text-decoration: underline;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "#mainplayer {" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= " border-radius: 8px;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        If Not old_ie Then sent_css &= " object-fit: center;" & vbCrLf
                        If Not old_ie Then sent_css &= " margin-left: auto;" & vbCrLf
                        If Not old_ie Then sent_css &= " margin-right: auto;" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".red_button {" & vbCrLf
                        sent_css &= " color: white;" & vbCrLf

                        Select Case wanted_skin
                            Case "modern"
                                sent_css &= " background-color: #e01425;" & vbCrLf
                            Case "rose"
                                sent_css &= " background-color: rgb(178, 15, 120);" & vbCrLf
                                sent_css &= " background-image: url('btn_pink.png');" & vbCrLf
                            Case "aqua"
                                sent_css &= " background-image: url('btn_aqua.png');" & vbCrLf
                                sent_css &= " background-color: #1f38a0;" & vbCrLf
                            Case "monochrome"
                                sent_css &= " background-color: black;" & vbCrLf
                                sent_css &= " border: 1px solid black;" & vbCrLf
                            Case Else
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
                                sent_css &= " background-color: white;" & vbCrLf
                                sent_css &= " color: black;" & vbCrLf
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

                        'Pseudo-obfuscation du code et économie en espace
                        'sent_css = sent_css.Replace(" ", String.Empty).Replace(vbCrLf, String.Empty)

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

                        sent_page &= "<BR><CENTER><A HREF=""/"">Retour à l'index</A> - <A HREF=""/config.cgi"">Paramètres du client</A> - <A HREF=""/about.htm"">Informations sur RetroYT</A></CENTER><BR><BR>" & vbCrLf

                        sent_page &= "<B>Ressources picturales utilisées par le serveur:</B><BR>" & vbCrLf

                        For Each f As String In IO.Directory.GetFiles(CurDir() & "\resfiles")
                            f = f.Remove(0, CStr(CurDir() & "\resfiles\").Length)
                            If (f <> "nopic.jpg" And f <> "qtplugin.cab") Then sent_page &= "<IMG SRC=""" & f & """ ALT=""" & f & """ />" & vbCrLf
                        Next

                        sent_page &= "<BR><BR>" & vbCrLf
                        sent_page &= "<B>Cookie utilisateur:</B> " & EscapeHtml(current_cookie) & "<BR>" & vbCrLf
                        sent_page &= "<B>Hôte HTTP:</B> " & EscapeHtml(last_host) & "<BR>" & vbCrLf
                        sent_page &= "<B>Agent utilisateur:</B> " & EscapeHtml(ua_string) & "<BR>" & vbCrLf

                        sent_page &= "<B>Format multimédia utilisé:</B> " & vbCrLf

                        Select Case used_codec
                            Case "mpeg1"
                                'Codec vidéo MPEG-1, audio MP2
                                sent_page &= "Conteneur MPEG (*.mpg), codec vidéo: MPEG-1 (1,15MBPS de bitrate), codec audio: MP2 (96KBPS, 2 canaux stéréo @ 44,1KHz), tampon mémoire: 320Ko"
                            Case "avi_mpeg4"
                                'Format AVI encodé avec MPEG-4 (codec vidéo assez fonctionnel et compatible avec les systèmes Windows), et MP3.
                                sent_page &= "Conteneur AVI (Microsoft), codec vidéo: MS MPEG4v2 (500KBPS de bitrate), codec audio: MP3 (96KBPS)"
                            Case "avi_yuv"
                                'Format AVI YUV (sans codec) avec PCM
                                sent_page &= "Conteneur AVI (Microsoft), vidéo YUV (YUY2), audio PCM (1 canal mono @ 44,1KHz, 16-bits signés, little endian)"
                            Case "wmv2"
                                'Format WMV, très utilisé sous Windows, depuis Windows 98. Codec WMV2 et WMAv2
                                sent_page &= "Conteneur WMV (Microsoft), codec vidéo: WMV2 (800KBPS de bitrate), codec audio: WMAv2 (96KBPS)"
                            Case "wmv1"
                                'Format WMV ancien, codec WMV2, audio WMAv1.
                                sent_page &= "Conteneur WMV (Microsoft), codec vidéo: WMV1 (500KBPS de bitrate), codec audio: WMAv1 (64KBPS, 1 canal mono @ 44,1KHz)"
                            Case "rm"
                                'Format Real Media (code par Le Jarb aidé de Léo AI). A permis de faire fonctionner la lecture intégrée sous IE 3.0 et Windows 3.11.
                                'Codec vidéo RV10 et audio AC3
                                sent_page &= "Conteneur Real Media (*.rm), codec vidéo: RV10 (640KBPS de bitrate), codec audio: AC3 (64KBPS)"
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
                                sent_page &= "Conteneur MOV (Apple QuickTime), codec vidéo: MPEG-4 (500KBPS), codec audio: MP3 (96KBPS, 2 canaux stéréo @ 44,1KHz)"
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
                                sent_page &= "Conteneur MP4, codec vidéo: H.264 AVC (MPEG-4 Part 10), codec audio: AAC"
                            Case "xvid"
                                sent_page &= "Conteneur AVI, codec vidéo: Xvid, codec audio: MP3 (128KBPS)"
                            Case "flv"
                                'Format FLV (Codec vidéo Sorenson Spark, audio MP3) [Macromedia Flash Video]
                                sent_page &= "Conteneur Macromedia Flash Video (*.flv), codec vidéo: Sorenson Spark (500KBPS de bitrate), codec audio: MP3 (96KBPS)"
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

                        sent_page &= "<B>Nombre de résultats par recherche:</B> " & number_of_results.ToString & " résultat(s)" & vbCrLf

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
                    Case Else
                        'En cas de ressource introuvable, ou inutilisée par le serveur
                        If arg.Length > 40 Then
                            arg = arg.Substring(0, 40) & "..."
                        End If

                        WriteLog("Ressource demandée introuvable: " & arg, , client)

                        Dim notfound_data As Byte() = GetHTTPBytes(404, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 404 - Ressource introuvable</H1>" & vbCrLf & "<P>Le fichier '<I>/" & arg & "</I>' n'a pas été trouvé sur ce serveur.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à la page d'index.</P>" & vbCrLf)

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

            Dim baddata As Byte() = GetHTTPBytes(400, "<TITLE>RetroYT - Erreur</TITLE><H1>Erreur 400 - Requête erronée</H1>" & vbCrLf & "<P>Requête HTTP invalide ou malformée.<BR><BR>" & vbCrLf & "Cliquez <A HREF=""/"">ici</A> pour retourner à l'index.</P>" & vbCrLf)

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
