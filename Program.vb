Imports System.Data
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Module Program

    'Projet RetroYT codé par Monokeros en 2026
    'Tous droits réservés. Licence freeware/open source.

    Public port As Integer = 80 'Port à écouter pour créer le serveur
    Public patternpage As String = Nothing 'Page HTML modèle à renvoyer au client
    Public last_titles As New Dictionary(Of String, String) 'Cache des vidéos lues/recherchées avec leur titre
    Public video_dimensions As New Dictionary(Of String, String) 'Cache de la taille des vidéos lues/recherchées
    Public last_view As String = Nothing 'Identifiant de la vidéo en cours de lecture
    Public iso As Encoding = Encoding.GetEncoding("iso-8859-1")
    Public last_host As String = String.Empty
    Public link_color As String = "#800000"
    Public range_begin As Long = -1
    Public range_end As Long = -1

    Public list_used_player() As String = {"no_integration", "legacy_wmp", "wmp", "embed", "video", "realplayer", "activex_realplayer", "embed_vlc", "vlc", "alt_vlc", "quicktime", "embed_quicktime", "flash", "embed_flash", "activex_flash", "object"}
    Public list_skin() As String = {"oldyt", "cosmic", "dark", "modern", "rose", "aqua"}
    Public list_playersize() As String = {"auto", "micro", "middle", "ultrasmall", "small", "large", "cinema", "autoheight"}
    Public list_usedcodec() As String = {"mpeg1", "avi_mpeg4", "avi_msvideo1", "mp4", "rm", "wmv2", "mov_cinepak", "mov_svq1", "3gp", "avi_yuv", "flv", "wmv1", "mov_mpeg4", "avi_cinepak", "mov_rpza"}
    Public list_framerate() As String = {"auto", "10", "12", "15", "20", "24", "25", "30", "60"}
    Public list_resolution() As String = {"auto", "96p", "120p", "144p", "240p", "360p", "480p", "720p", "1080p"}
    Public list_results() As String = {"5", "10", "20"}

    Public http_status_labels(1024) As String

    'Pied de page générique à certaines pages.
    Public footer As String = "<HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & "<P ALIGN=CENTER><B>RetroYT</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.<BR>Ce projet n'est pas affilié avec cette entreprise. <A HREF=""/about.htm"" STYLE=""color: " & link_color & """>Plus d'informations sur RetroYT</A>.</P>" & vbCrLf & "</BODY>" & vbCrLf & "</HTML>" & vbCrLf
    Public Const cookie_header As String = "retroyt="

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

    Sub InitValues(Optional ByVal t As String = Nothing, Optional ByVal k As String = Nothing, Optional ByVal wanted_skin As String = "cosmic")
        System.Threading.Thread.Sleep(100)
        'Cette fonction génère une entête et un corps de page HTML de base à retourner au client.
        patternpage = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
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
                patternpage &= "<BODY COLOR=""#FFFFFF"" BGCOLOR=""#000000"" ALINK=""#FFFFFF"" VLINK=""#FFFFFF"">" & vbCrLf
                link_color = "#c2272f"
            Case "cosmic"
                patternpage &= "<BODY COLOR=""#000000"" BGCOLOR=""#EAEAEA"" ALINK=""#B6262C"" VLINK=""#B6262C"" BACKGROUND=""cosmic.gif"">" & vbCrLf
                link_color = "#c2272f"
            Case "rose"
                patternpage &= "<BODY COLOR=""#100010"" BGCOLOR=""#F2DEF2"" ALINK=""#800080"" VLINK=""#800080"">" & vbCrLf
                link_color = "#a0046b"
            Case "aqua"
                patternpage &= "<BODY COLOR=""#0000F0"" BGCOLOR=""#ECFFFF"" ALINK=""#2037A0"" VLINK=""#2037A0"">" & vbCrLf
                link_color = "#1f38a0"
            Case Else
                patternpage &= "<BODY COLOR=""#000000"" BGCOLOR=""#FFFFFF"" ALINK=""#B6262C"" VLINK=""#B6262C"">" & vbCrLf
                link_color = "#c2272f"
        End Select

        Dim used_logo As String = "yt_logo2.gif"

        Select Case wanted_skin
            Case "oldyt" : used_logo = "yt_logo.gif"
            Case "cosmic" : used_logo = "yt_logo2.gif"
            Case "dark" : used_logo = "yt_dark.gif"
            Case "rose" : used_logo = "yt_rose.gif"
            Case "aqua" : used_logo = "yt_aqua.gif"
            Case Else : used_logo = "yt_modrn.gif"
        End Select

        'La tête de page pour rechercher des vidéos. Ce formulaire est présent sur chaque page naviguée.
        patternpage &= " <FORM METHOD=""GET"" ACTION=""/search"">" & vbCrLf
        patternpage &= " <CENTER><TABLE BORDER=0 WIDTH=780 ALIGN=CENTER>" & vbCrLf
        patternpage &= "  <TR>" & vbCrLf
        patternpage &= "   <TD WIDTH=90>&nbsp;</TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=120><A HREF=""/""><IMG SRC=""" & used_logo & """ BORDER=0 ALT=""Logo RetroYT"" HEIGHT=44 /></A></TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=330>&nbsp;&nbsp;<INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 310px;"" WIDTH=320 MAXLENGTH=256 /></TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=*><INPUT TYPE=""SUBMIT"" VALUE="" Rechercher "" WIDTH=400 CLASS=""red_button"" /> &nbsp; <A HREF=""/config.cgi"" STYLE=""color: " & link_color & ";"">Paramètres</A></TD>" & vbCrLf
        patternpage &= "  </TR>" & vbCrLf
        patternpage &= " </TABLE></CENTER>" & vbCrLf
        patternpage &= " </FORM><BR><BR><HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & vbCrLf

        footer = "<HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & "<P ALIGN=CENTER><B>RetroYT</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.<BR>Ce projet n'est pas affilié avec cette entreprise. <A HREF=""/about.htm"" STYLE=""color: " & link_color & ";"">Plus d'informations sur RetroYT</A>.</P>" & vbCrLf & "</BODY>" & vbCrLf & "</HTML>" & vbCrLf
    End Sub

    Sub UpdateCache()
        Dim cache_dir As String = CurDir() & "\vidcache"

        If Not Directory.Exists(cache_dir) Then Exit Sub

        Dim files As List(Of FileInfo) = Directory.GetFiles(cache_dir).
        Select(Function(f) New FileInfo(f)).
        OrderBy(Function(fi) fi.LastWriteTime).ToList()

        Dim files_length As Long = files.Sum(Function(f) f.Length)

        Dim minFree As Long = 134217728 '132Mo
        Dim maxCache As Long = 17179869184 '16Go

        Dim freeSpace As Long = 0

        For Each c As IO.DriveInfo In IO.DriveInfo.GetDrives()
            If LCase(CurDir()).StartsWith(LCase(c.RootDirectory.ToString)) Then
                freeSpace = c.AvailableFreeSpace
                Exit For
            End If
        Next

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
    End Sub

    Sub CleanupLock()
        'Suppression du fichier output.lock, coupure du processus ffmpeg.exe associé, et suppression du fichier qui était en cours de création (pour éviter les conflits)
        Try
            If IO.File.Exists(CurDir() & "\output.lock") Then
                Dim proc_content As String = IO.File.ReadAllText(CurDir() & "\output.lock")

                If proc_content.Contains(vbCrLf) Then
                    Dim proc_c() As String = proc_content.Split(vbCrLf)

                    If IsNumeric(proc_c(0)) And IO.File.Exists(CurDir() & "\vidcache\" & proc_c(1)) Then
                        Dim proc_id As Integer = CInt(proc_c(0))
                        Dim proc_delfile As String = CurDir() & "\vidcache\" & proc_c(1)
                        For Each pr1 As Process In System.Diagnostics.Process.GetProcesses
                            If pr1.Id = proc_id Then
                                pr1.Kill()
                                WriteLog("Un ancien processus de ffmpeg toujours en exécution a été trouvé. Il a été arrêté de force.", ConsoleColor.Blue)
                                Exit For
                            End If
                        Next
                        If IO.File.Exists(proc_delfile) Then IO.File.Delete(proc_delfile)
                    End If
                End If

                IO.File.Delete(CurDir() & "\output.lock")
            End If
        Catch ex As Exception
            WriteLog("Impossible d'arrêter un ancien processus de ffmpeg: " & ex.Message, ConsoleColor.Red)
        End Try
    End Sub

    Function GetClientIP(client As TcpClient) As String
        'Obtenir l'adresse IP du client
        Return CType(client.Client.RemoteEndPoint, IPEndPoint).Address.ToString()
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
        Dim months() As String = {"0", "jan.", "fév.", "mars", "avr.", "mai", "juin", "juill.", "août", "sept.", "oct.", "nov.", "déc."}
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
            IO.File.AppendAllText("srvlogs\rs_" & DateTime.Now.ToString("dd-MM-yyyy") & ".log", f & vbCrLf)
        Catch ex As Exception

        End Try
    End Sub

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

        WriteLog("Erreur #" & status.ToString & " (" & http_status_labels(status) & ") renvoyée au client.", ConsoleColor.Red)

        Return iso.GetBytes(http_response)
    End Function

    Sub Main(args As String())

        For i As Integer = 0 To 1024
            http_status_labels(i) = "No Message Provided"
        Next

        http_status_labels(200) = "OK"
        http_status_labels(301) = "Moved Permanently"
        http_status_labels(302) = "Found"
        http_status_labels(400) = "Bad Request"
        http_status_labels(401) = "Unauthorized"
        http_status_labels(403) = "Forbidden"
        http_status_labels(404) = "Not Found"
        http_status_labels(410) = "Gone"
        http_status_labels(413) = "Content Too Large"
        http_status_labels(414) = "URI Too Long"
        http_status_labels(415) = "Unsupported Media Type"
        http_status_labels(416) = "Range Not Satisfiable"
        http_status_labels(500) = "Internal Server Error"
        http_status_labels(501) = "Not Implemented"
        http_status_labels(502) = "Bad Gateway"
        http_status_labels(507) = "Insufficient Storage"

        'L'application démarre ici!
        Console.Title = "RetroYT"

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("******************************")
        Console.WriteLine("*      RetroYT Bêta 3.3      *")
        Console.WriteLine("******************************")
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

        If Not IO.Directory.Exists(CurDir() & "\srvlogs") Then
            IO.Directory.CreateDirectory(CurDir() & "\srvlogs")
        End If

        'Nettoyer les fichiers en cours de décodage
        CleanupLock()
        UpdateCache()

        Console.WriteLine()
        WriteLog("Serveur lancé sur le port " & port.ToString & " avec succès! En attente de connexions entrantes...")

        If port = 80 Then
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1/")
        Else
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez en local sur http://127.0.0.1:" & port.ToString & "/")
        End If

        WriteLog("Veuillez appuyer sur CTRL + C pour arrêter le serveur.")
        Console.WriteLine()

        While True
            Dim client = listener.AcceptTcpClient()
            Dim t As New Threading.Thread(Sub() HandleClient(client))
            t.Start()
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
                bytes.Add(32) ' espace
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
        Dim ultra_legacy As Boolean = False
        Dim frame_rate As String = "25"
        Dim wanted_skin As String = "cosmic"
        Dim number_of_results As Integer = 10
        Dim http_ver As String = "1.0"

        'Prise en charge des requêtes par le client
        System.Threading.Thread.Sleep(50)
        Dim stream = client.GetStream()

        'Lire la requête HTTP
        Dim buffer(8192) As Byte
        Dim bytesRead = stream.Read(buffer, 0, buffer.Length)
        Dim request As String = iso.GetString(buffer, 0, bytesRead)
        range_begin = -1
        range_end = -1
        number_of_results = 10

        If String.IsNullOrEmpty(request) Then
            Dim baddata As Byte() = GetHTTPBytes(400, "<h1>Error 400 - Bad Request</h1>" & vbCrLf & "<p>HTTP request was empty.</p>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
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
        End If

        For Each l As String In header_list
            Dim tete, corps As String
            If l.Contains(":") Then
                tete = l.Substring(0, l.IndexOf(":")).Trim 'l.Split(":")(0).Trim
                corps = l.Substring(l.IndexOf(":") + 1, l.Length - 1 - l.IndexOf(":")).Trim 'l.Split(":")(1).Trim

                For x As Integer = 0 To &H1F 'Retirer les caractères systèmes, souvent synonymes de tentatives de hack mémoire
                    corps = corps.Replace(Chr(x), String.Empty)
                Next

                Select Case LCase(tete)
                    Case "cookie"
                        If corps.Contains("=") And corps.Contains("&") And corps.StartsWith(cookie_header) Then
                            'WriteLog("Cookie envoyé par le client: " & corps, ConsoleColor.Yellow, client)

                            corps = corps.Remove(0, cookie_header.Length) 'Retirer retroyt=
                            If corps.Contains(";path=/") Then corps = corps.Remove(";path=/")
                            Dim c_params() As String = Split(corps, "&")

                            For Each c_param As String In c_params
                                Dim p1, p2 As String
                                p1 = LCase(c_param.Split("=")(0))
                                p2 = LCase(c_param.Split("=")(1))

                                Select Case p1
                                    Case "usedplayer"
                                        If list_used_player.Contains(p2) Then
                                            used_player = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "playersize"
                                        If list_playersize.Contains(p2) Then
                                            player_size = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "usedcodec"
                                        If list_usedcodec.Contains(p2) Then
                                            used_codec = p2
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "framerate"
                                        If list_framerate.Contains(p2) Then
                                            If p2 = "auto" Then
                                                frame_rate = -1
                                            Else
                                                frame_rate = CInt(p2)
                                            End If
                                        Else
                                            bad_cookie = True
                                        End If
                                    Case "usedresolution"
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
                                            number_of_results = CInt(p2)
                                        End If
                                    Case Else
                                        'Les autres paramètres sont considérés comme faux.
                                        bad_cookie = True
                                End Select
                            Next
                        End If
                    Case "host"
                        last_host = corps
                    Case "range"
                        If LCase(corps.StartsWith("bytes=")) Then
                            corps = corps.Remove(0, 5)
                            If corps.Contains(",") Then corps = corps.Substring(0, corps.IndexOf(","))

                            If corps = "-" Then
                                Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.</p>" & vbCrLf)

                                Try
                                    stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                Catch ey As Exception
                                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                End Try

                                client.Close()
                                Exit Sub
                            ElseIf corps.EndsWith("-") Then
                                'D'un offset indiqué, jusqu'à la fin du fichier
                                corps = corps.Replace("-", String.Empty)
                                Dim echec As Boolean = False

                                If String.IsNullOrEmpty(corps) Then
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.</p>" & vbCrLf)

                                    Try
                                        stream.Write(invalidrangedata, 0, invalidrangedata.Length)
                                    Catch ey As Exception
                                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ey.Message, ConsoleColor.Red)
                                    End Try

                                    client.Close()
                                    Exit Sub
                                Else
                                    If IsNumeric(corps) Then
                                        Try
                                            Dim tmp_offset As Long = CLng(corps.Trim)

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
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.</p>" & vbCrLf)

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
                            ElseIf corps.StartsWith("-") Then
                                'Les X derniers octets

                                corps = corps.Remove(0, 1)
                                Dim echec As Boolean = False

                                If IsNumeric(corps) Then
                                    Try
                                        Dim end_offset As Long = CLng(corps)

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
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car l'offset demandé dans le fichier est invalide.</p>" & vbCrLf)

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
                                If corps.Contains("-") Then
                                    Dim param1, param2 As String
                                    param1 = corps.Split("-")(0)
                                    param2 = corps.Split("-")(1)

                                    If Not IsNumeric(param1) Or Not IsNumeric(param2) Then
                                        'Lever une erreur
                                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.</p>" & vbCrLf)

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
                                            Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.</p>" & vbCrLf)

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
                                    Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.</p>" & vbCrLf)

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
            Dim result_page As String = "<h1>Error 400 - Bad Request</h1><p>Your client cookie was invalid, so it has been reset.<br><br>Please go back to the <a href=""/"">index</a> of the website.</p>" & vbCrLf
            WriteLog("Error #400: Client cookie was invalid and has been reset.", ConsoleColor.Yellow, client)

            Dim index_resp As String =
                "HTTP/" & http_ver & " 400 Bad Request" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Set-Cookie: " & cookie_header & "results=10&playersize=middle&usedcodec=avi_mpeg4&usedplayer=embed&skin=cosmic&usedresolution=auto&framerate=auto;path=/" & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
            Exit Sub
        End If

        'Ecriture de la commande dans les rapports de connexion
        'WriteLog("[" & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & "] Requête entrante détectée en provenance de l'adresse IP " & GetClientIP(client) & "...", ConsoleColor.White)

        'Erreur 414 - URL trop longue
        If Not String.IsNullOrEmpty(request) Then
            If request.Length > 4 Then
                Dim uri_arg As String = Split(request)(1)
                If uri_arg.Length > 512 Then
                    WriteLog("Erreur HTTP 414: URL trop longue.", , client)
                    Dim toolongdata As Byte() = GetHTTPBytes(414, "<h1>Error 414 - URI Too Long</h1>" & vbCrLf & "<p>Veuillez envoyer une URL plus courte.</p>" & vbCrLf)

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
            WriteLog("Erreur HTTP 413: Contenu trop grand envoyé.", , client)
            Dim toomuchdata As Byte() = GetHTTPBytes(414, "<h1>Error 413 - Content Too Large</h1>" & vbCrLf & "<p>Trop de données communiquées au serveur.</p>" & vbCrLf)

            Try
                stream.Write(toomuchdata, 0, toomuchdata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
            Exit Sub
        ElseIf String.IsNullOrEmpty(request) Then
            'Requête vide
            WriteLog("Erreur HTTP 400: Requête vide envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<h1>Error 400 - Bad Request</h1>" & vbCrLf & "<p>HTTP request was empty.</p>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
            End Try

            client.Close()
        ElseIf request.StartsWith("GET /watch?v=") Then
            'Demande de lecture d'une vidéo par le client
            Dim watcharg As String = Split(request)(1)
            watcharg = watcharg.Remove(0, 9)

            UpdateCache()

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
                                        Case "usedcodec"
                                            If list_usedcodec.Contains(single_params(1)) Then used_codec = single_params(1)
                                        Case "usedresolution"
                                            If list_resolution.Contains(single_params(1)) Then used_resolution = single_params(1)
                                        Case "framerate"
                                            If list_framerate.Contains(single_params(1)) Then frame_rate = single_params(1)
                                        Case "usedplayer"
                                            If list_used_player.Contains(single_params(1)) Then used_player = single_params(1)
                                        Case "playersize"
                                            If list_playersize.Contains(single_params(1)) Then player_size = single_params(1)
                                        Case "legacy"
                                            If single_params(1) = "true" Then
                                                player_size = "small"
                                                used_codec = "avi_msvideo1"
                                                used_player = "legacy_wmp"
                                                used_resolution = "240p"
                                                frame_rate = "15"
                                                ultra_legacy = True
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
                    Case "mpeg1", "avi_msvideo1", "rm", "avi_yuv", "mov_rpza" : num_frame_rate = 15
                    Case "3gp", "avi_cinepak", "mov_cinepak" : num_frame_rate = 10
                    Case "wmv2", "mp4", "mov_svq1", "avi_mpeg4", "wmv1", "mov_mpeg4" : num_frame_rate = 25
                    Case "flv" : num_frame_rate = 24
                    Case Else : num_frame_rate = 25
                End Select
            Else
                num_frame_rate = CInt(frame_rate)
            End If

            If used_resolution = "auto" Then
                Select Case used_codec
                    Case "avi_mpeg4", "wmv2", "mov_svq1", "flv", "wmv1", "mov_mpeg4" : num_used_resolution = 480
                    Case "avi_msvideo1", "mpeg1", "avi_yuv", "mov_rpza" : num_used_resolution = 240
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

            If used_codec = "avi_yuv" Then
                If num_used_resolution > 240 Then
                    num_used_resolution = 240 'Ne pas activer la HD ou SD sur AVI YUV, pour éviter de produire des fichiers énormes, qui exigeraient beaucoup de ressources.
                End If
                If num_frame_rate = 60 Then num_frame_rate = 30
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
                Case "flv" : tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p.flv"
                Case Else
                    'Fallback vers MPEG-4
                    num_used_resolution = 240
                    tmp_filename = "output_" & watcharg & "_" & num_used_resolution.ToString & "p_mpeg4.avi"
            End Select

            Dim a As Integer = tmp_filename.IndexOf("."c)
            output_filename = GetShortName(tmp_filename) & UCase(tmp_filename.Substring(a, tmp_filename.Length - a))
            output_path = CurDir() & "\vidcache\" & output_filename

            'Début du traitement de la requête. On vérifie si l'ID est valide (pas s'il existe).
            If LooksLikeYoutubeID(watcharg) Then
                last_view = watcharg

                WriteLog("Vidéo demandée: https://www.youtube.com/watch?v=" & last_view, ConsoleColor.Green, client)
                WriteLog("Résolution en " & num_used_resolution.ToString & "p @ " & num_frame_rate.ToString & " FPS demandée.", ConsoleColor.Green)

                If IsNetworkAvailable() Then
                    'Si la vidéo n'est pas en cache, le logiciel va interroger yt-dlp pour l'obtenir.
                    If Not IO.File.Exists(output_path) Then

                        WriteLog("Téléchargement au format MP4 (Codec vidéo H.264, codec audio M4A, en résolution " & num_used_resolution.ToString & "p" & num_frame_rate.ToString & ")...")
                        'Exécution du processus d'obtention de la vidéo souhaitée.

                        Dim freeSpace As Long = -1
                        For Each c As IO.DriveInfo In IO.DriveInfo.GetDrives()
                            If LCase(CurDir()).StartsWith(LCase(c.RootDirectory.ToString)) Then
                                freeSpace = c.AvailableFreeSpace
                                Exit For
                            End If
                        Next

                        If freeSpace >= 0 And freeSpace <= 134217728 Then
                            Dim baddata As Byte() = GetHTTPBytes(507, "<h1>Error 507 - Insufficient Disk Space</h1>" & vbCrLf & "<p>Il n'y a plus assez d'espace disque sur le serveur pour mettre en cache la vidéo demandée.</p>" & vbCrLf)

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
                            Dim intermed As Integer = num_used_resolution
                            'If intermed = 120 Then intermed = 144 'Le 120p n'existe pas sur YouTube
                            'If intermed = 96 Then intermed = 144 'Ni le 96p.
                            'If intermed <= 360 Then intermed = 480
                            Dim destfile As String = CurDir() & "\vidcache\" & UCase(GetShortName("output_" & watcharg & "_" & num_used_resolution.ToString & "p.mp4")) & ".MP4"

                            If Not IO.File.Exists(destfile) Then
                                'La commande suivante demande une vidéo au format MP4 (Codec vidéo H.264, audio M4A).

                                '-f ""bestvideo[height<=" & intermed.ToString & "]+bestaudio/best[height<=" & intermed.ToString & "]""
                                '""bv*[vcodec^=avc1][height<=" & intermed.ToString & "]+ba[ext=m4a]/b[height<=" & intermed.ToString & "][ext=mp4]""

                                psi.Arguments = "-f ""bv*[vcodec^=avc1][height<=" & intermed.ToString & "]+ba[ext=m4a]/b[height<=" & intermed.ToString & "][ext=mp4]"" --no-part --no-continue -o """ & destfile & """ ""https://www.youtube.com/watch?v=" & last_view & """"
                                psi.UseShellExecute = False
                                psi.CreateNoWindow = True
                                psi.RedirectStandardOutput = True
                                psi.RedirectStandardError = True

                                'Call Process.Start(psi)
                                Dim p As Process = Process.Start(psi)
                                Dim output = p.StandardOutput.ReadToEnd()
                                Dim err = p.StandardError.ReadToEnd()
                                p.WaitForExit()

                                'Affichage du résultat dans la fenêtre
                                WriteLog(output, ConsoleColor.Cyan)
                                If String.IsNullOrEmpty(err) AndAlso err.Length > 0 Then WriteLog(err, ConsoleColor.Red)
                            Else
                                WriteLog("La vidéo a déjà été téléchargée, et est disponible en cache.")
                            End If

                            'Code pansement nécessaire pour ajouter ou retirer des extensions face aux caprices de yt-dlp, qui en ajoute ou en retire selon son bon vouloir.
                            For Each i As String In IO.Directory.GetFiles(CurDir() & "\vidcache")
                                Dim i_path() As String = Split(i, "\")
                                Dim i_end As String = i_path(i_path.Length - 1)

                                If i_end.Contains(".") = False Then
                                    IO.File.Move(i, i & ".MP4") 'Ajouter .MP4
                                ElseIf i_end.EndsWith(".MP4.mp4") Then
                                    IO.File.Move(i, i.Remove(i.Length - 4, 4)) 'Virer la mention .mp4 de trop
                                End If
                            Next

                            If Not IO.File.Exists(destfile) Then
                                Dim ise_data As Byte() = GetHTTPBytes(500, "<h1>Error 500 - Internal Server Error</h1>" & vbCrLf & "<p>The requested video could not be downloaded (Known ID: <i>" & last_view & "</i>). Please try another input format/resolution.</p>" & vbCrLf)

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
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format MPEG (Codec vidéo MPEG-1, codec audio MP2)...")
                                    num_used_resolution = 360
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=352:240 -r 30000/1001 -c:v mpeg1video -b:v 1150k -maxrate 1150k -minrate 1150k -bufsize 327680 -c:a mp2 -b:a 96k -ar 44100 -ac 2 """ & output_path & """"
                                Case "avi_mpeg4"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format AVI (Codec vidéo MPEG-4, codec audio MP3)...")
                                    'Format AVI encodé avec MPEG-4 (codec vidéo assez fonctionnel et compatible avec les systèmes Windows), et MP3.
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 96k """ & output_path & """"
                                Case "avi_yuv"
                                    'Format AVI YUV (sans codec) avec PCM
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format AVI (Vidéo YUV, codec audio PCM)...")
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v rawvideo -pix_fmt yuyv422 -vtag YUY2 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                Case "wmv2"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format WMV nouveau (Codec vidéo WMV2, codec audio WMAv2)...")
                                    'Format WMV, très utilisé sous Windows, depuis Windows 98. Codec WMV2 et WMAv2
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv2 -b:v 800k -c:a wmav2 -b:a 96k """ & output_path & """"
                                Case "wmv1"
                                    'Format WMV ancien, codec WMV2, audio WMAv1.
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format WMV ancien (Codec vidéo WMV1, codec audio WMAv1)...")
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v wmv1 -b:v 500k -c:a wmav1 -b:a 64k -ar 44100 -ac 1 """ & output_path & """"
                                Case "rm"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format RealMedia (Codec vidéo RV10, codec audio MP2)...")
                                    'Format Real Media (code par Le Jarb aidé de Léo AI). A permis de faire fonctionner la lecture intégrée sous IE 3.0 et Windows 3.11.
                                    'Codec vidéo RV10 et audio MP2
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
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format 3GP (Codec vidéo H.263, codec audio AMR-NB)...")
                                    If num_used_resolution = 96 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=128:96 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """"
                                    Else
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=176:144 -r " & num_frame_rate.ToString & " -c:v h263 -b:v 128k -c:a libopencore_amrnb -b:a 12.2k -ar 8000 -ac 1 """ & output_path & """"
                                    End If
                                Case "mov_cinepak"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format Apple QuickTime (Codec vidéo Cinepak, codec audio PCM)...")
                                    'Format QuickTime (codec vidéo Cinepak, fortement utilisé dans les années 1990, et PCM pour l'audio)
                                    If num_used_resolution <= 120 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    ElseIf num_used_resolution = 144 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    Else
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -q:v 3 -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    End If
                                Case "mov_svq1"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format Apple QuickTime (Codec vidéo Sorenson SVQ1, codec audio PCM)...")
                                    'Format QuickTime (codec vidéo Sorenson SVQ1, surtout utilisé dans les années 2000, et codec audio MP3)
                                    If num_used_resolution >= 720 Then num_used_resolution = 480 'HQ indisponible
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v svq1 -q:v 3 -c:a libmp3lame -b:a 64k """ & output_path & """"
                                Case "mov_mpeg4"
                                    'Format QuickTime (codec vidéo MPEG-4, audio MP3)
                                    If num_used_resolution >= 720 Then num_used_resolution = 480 'HQ indisponible
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format Apple QuickTime (Codec vidéo MPEG-4, codec audio MP3)...")
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v mpeg4 -b:v 500k -c:a libmp3lame -b:a 96k -ar 44100 -ac 2 """ & output_path & """"
                                Case "mov_rpza"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format Apple QuickTime (Codec vidéo RPZA, codec audio PCM)...")
                                    'Format QuickTime (codec vidéo RPZA, format très Apple des années 1990, et PCM pour l'audio)
                                    If num_used_resolution <= 120 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    ElseIf num_used_resolution = 144 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    Else
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v rpza -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    End If
                                Case "avi_msvideo1"
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format AVI (Codec vidéo MSVideo1, codec audio PCM)...")
                                    'Format AVI encodé avec Microsoft Video 1 (fonctionne en pratique sous toutes les versions de Windows, y compris Windows 3.11, surtout accompagné du codec audio PCM).
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
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format AVI (Codec vidéo Cinepak, codec audio PCM)...")
                                    'Format AVI encodé avec Cinepak (codec répandu dans les années 90, et pris en charge par Windows 3.11, surtout accompagné du codec audio PCM).
                                    If num_used_resolution <= 120 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=160:120 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    ElseIf num_used_resolution = 144 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=256:144 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    ElseIf num_used_resolution = 240 Then
                                        psi2.Arguments = "-i """ & destfile & """ -vf scale=320:240 -r " & num_frame_rate.ToString & " -c:v cinepak -c:a pcm_s16le -ar 44100 -ac 1 """ & output_path & """"
                                    End If
                                Case "mp4"
                                    'Format MP4 sans conversion directe
                                    WriteLog("Usage du format MP4, aucune conversion n'est donc nécessaire.")
                                Case "flv"
                                    'Format FLV (Codec vidéo Sorenson Spark, audio MP3) [Macromedia Flash Video]
                                    WriteLog("Conversion du fichier MP4 trouvé vers le format vidéo Flash (Codec vidéo Sorenson Spark, codec audio MP3)...")
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v flv -b:v 500k -c:a libmp3lame -b:a 96k """ & output_path & """"
                                Case Else
                                    WriteLog("Aucun format de destination valide, choix du format AVI (Codec vidéo MPEG-4, codec audio MP3) par défaut...")
                                    'Par défaut, envoyer du MPEG4.
                                    psi2.Arguments = "-i """ & destfile & """ -vf scale=-2:" & num_used_resolution.ToString & " -r " & num_frame_rate.ToString & " -c:v msmpeg4v2 -b:v 500k -c:a mp3 -b:a 96k """ & output_path & """"
                            End Select

                            If used_codec <> "mp4" Then
                                psi2.UseShellExecute = False
                                psi2.CreateNoWindow = True

                                Dim p2 As Process = Process.Start(psi2)

                                Try
                                    IO.File.WriteAllText(CurDir() & "\output.lock", p2.Id.ToString & vbCrLf & output_filename)
                                    p2.WaitForExit()
                                    IO.File.Delete(CurDir() & "\output.lock")
                                Catch ex As Exception
                                    WriteLog("Erreur lors de la conversion: " & ex.Message, ConsoleColor.Red)
                                End Try
                            End If
                        End If
                    Else
                        WriteLog("Vidéo déjà en cache !")
                        WriteLog("Résolution en " & num_used_resolution.ToString & "p @ " & num_frame_rate.ToString & " FPS demandée.", ConsoleColor.Green)
                    End If

                    'Mise en cache du titre (et de l'ID)
                    Dim tmp_title As String = "(Titre inconnu)"

                    If last_titles.ContainsKey(watcharg) Then
                        tmp_title = last_titles(watcharg)
                    Else
                        'Choper le titre en ligne, s'il venait à manquer.
                        Dim psi3 As New ProcessStartInfo()
                        psi3.FileName = "yt-dlp.exe"
                        psi3.Arguments = "--print ""%(title)s"" --no-warnings ""https://www.youtube.com/watch?v=" & watcharg & """ --encoding utf-8"

                        psi3.UseShellExecute = False
                        psi3.RedirectStandardOutput = True
                        psi3.RedirectStandardError = True
                        psi3.CreateNoWindow = True
                        psi3.StandardOutputEncoding = Encoding.UTF8
                        psi3.StandardErrorEncoding = Encoding.UTF8

                        Dim p3 As Process = Process.Start(psi3)
                        Dim output3 As String = p3.StandardOutput.ReadToEnd()
                        Dim err3 As String = p3.StandardError.ReadToEnd()
                        tmp_title = CleanText(output3)
                        last_titles.Add(watcharg, tmp_title)
                        If Not video_dimensions.ContainsKey(watcharg) Then video_dimensions.Add(watcharg, "640:480")

                        p3.WaitForExit()
                    End If

                    InitValues(tmp_title, , wanted_skin)
                    patternpage &= "<CENTER><DIV WIDTH=780 ALIGN=CENTER><BR>" & vbCrLf
                    patternpage &= "<P ALIGN=CENTER><B><FONT SIZE=4>" & EscapeHtml(tmp_title) & "</FONT></B></P><BR>" & vbCrLf

                    Dim media_type As String = "video/mp4"

                    Select Case used_codec
                        Case "mp4" : media_type = "video/mp4"
                        Case "rm" : media_type = "application/vnd.rn-realmedia"
                        Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak" : media_type = "video/x-msvideo"
                        Case "wmv1", "wmv2" : media_type = "video/x-ms-wmv"
                        Case "mov_cinepak", "mov_svq1", "mov_mpeg4", "mov_rpza" : media_type = "video/quicktime"
                        Case "3gp" : media_type = "video/3gpp"
                        Case "mpeg1" : media_type = "video/mpeg"
                        Case "flv" : media_type = "video/x-flv"
                        Case Else : media_type = "application/octet-stream"
                    End Select

                    Dim player_width, player_height As Integer
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
                        Case "middle"
                            'Moyen lecteur (correspondant au standard VGA, par défaut)
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

                            If video_dimensions.ContainsKey(watcharg) Then
                                Dim tmp_dimensions() As String = Split(video_dimensions(watcharg), ":")
                                tmp_w = CInt(tmp_dimensions(0))
                                tmp_h = CInt(tmp_dimensions(1))
                            End If

                            'Utilisation du Javascript pour redimensionner de façon dynamique le lecteur intégré.
                            patternpage &= "<script>" & vbCrLf
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
                    End Select

                    'Marge pour les contrôles
                    If used_player <> "video" Then player_height += 30

                    patternpage &= vbCrLf

                    'Le lecteur intégré
                    Select Case used_player
                        Case "legacy_wmp"
                            'Ancien lecteur Windows Media (6.4) intégré avec la balise <object> (ActiveX).
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour Windows Media Player 6.4 -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ classid=""CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95"">" & vbCrLf
                            patternpage &= " <param name=""FileName"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""AutoStart"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""EnableFullScreenControls"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""VideoBorder3D"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""StretchToFit"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""ShowControls"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""DisplaySize"" value=4>" & vbCrLf
                            patternpage &= " <param name=""DefaultFrame"" value=""" & GetHost() & "thumbnail?t=" & last_view & """>" & vbCrLf
                            patternpage &= "</object>" & vbCrLf
                        Case "wmp"
                            'Nouveau lecteur Windows Media (7.0 et +) intégré avec la balise <object> (ActiveX).
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour Windows Media Player 7.0 et plus -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ classid=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">" & vbCrLf
                            patternpage &= " <param name=""URL"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""AutoStart"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""EnableFullScreenControls"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""VideoBorder3D"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""StretchToFit"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""ShowControls"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""DefaultFrame"" value=""" & GetHost() & "thumbnail?t=" & last_view & """>" & vbCrLf
                            patternpage &= "</object>" & vbCrLf
                        Case "vlc"
                            'Lecteur VLC Media Player (via ActiveX)
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur VLC -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" classid=""CLSID:9BE31822-FDAD-461B-AD51-BE1D1C159921"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """>" & vbCrLf
                            patternpage &= " <param name=""target"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""MRL"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""src"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""autoplay"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""controller"" value=""true"">" & vbCrLf 'Affichage des contrôles du lecteur
                            patternpage &= " <param name=""loop"" value=""false"">" & vbCrLf
                            patternpage &= "</object>" & vbCrLf
                        Case "alt_vlc"
                            'Lecteur VLC Media Player (via ActiveX aussi)
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur VLC avec un identificateur de classe alternatif -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" classid=""CLSID:E23FE9C6-778E-49D4-B537-38FCDE4887D8"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """>" & vbCrLf
                            patternpage &= " <param name=""target"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""MRL"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""src"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""autoplay"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""controller"" value=""true"">" & vbCrLf 'Affichage des contrôles du lecteur
                            patternpage &= " <param name=""loop"" value=""false"">" & vbCrLf
                            patternpage &= "</object>" & vbCrLf
                        Case "embed_vlc"
                            'Lecteur VLC via embed.
                            patternpage &= "<!-- Embarcation du plugin VLC -->" & vbCrLf & vbCrLf
                            patternpage &= "<embed id=""mainplayer"" type=""application/x-vlc-plugin"" src=""" & GetHost() & "v/" & output_filename & """ target=""" & GetHost() & "v/" & output_filename & """ mrl=""" & GetHost() & "v/" & output_filename & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autoplay=""true"" loop=""false"" />" & vbCrLf
                        Case "quicktime"
                            'Lecteur QuickTime via ActiveX (Exclusivement sous Windows)
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur Apple QuickTime -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" classid=""CLSID:02BF25D5-8C17-4B23-BC80-D3488ABDDC6B"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ codebase=""http://www.apple.com/qtactivex/qtplugin.cab"">" & vbCrLf
                            patternpage &= " <param name=""src"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= " <param name=""autoplay"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""controller"" value=""true"">" & vbCrLf
                            patternpage &= "</object>" & vbCrLf & vbCrLf
                        Case "embed_quicktime"
                            'Lecteur QuickTime via la balise embed (surtout pour les systèmes Apple)
                            patternpage &= "<!-- Embarcation d'un lecteur Apple QuickTime -->" & vbCrLf & vbCrLf
                            patternpage &= "<embed id=""mainplayer"" src=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ controller=""true"" autoplay=""true"" />" & vbCrLf
                        Case "embed"
                            'Balise <embed> générique, une syntaxe et un fonctionnement lancés par NetScape en 1995.
                            patternpage &= "<!-- Embarcation du contenu multimédia avec la balise embed. -->" & vbCrLf & vbCrLf
                            If used_codec = "rm" Then
                                patternpage &= "<embed id=""mainplayer"" src=""" & GetHost() & "v/" & output_filename & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ type=""audio/x-pn-realaudio-plugin"" autostart=""true"" controls=""ImageWindow"" console=""rmplayer"" /><br>" & vbCrLf
                                patternpage &= "<embed width=""" & player_width.ToString & """ height=""20"" type=""audio/x-pn-realaudio-plugin"" controls=""PositionSlider"" console=""rmplayer"" />" & vbCrLf
                            Else
                                patternpage &= "<embed id=""mainplayer"" src=""" & GetHost() & "v/" & output_filename & """ mrl=""" & GetHost() & "v/" & output_filename & """ target=""" & GetHost() & "v/" & output_filename & """ href=""" & GetHost() & "v/" & output_filename & """ filename=""" & GetHost() & "v/" & output_filename & """ url=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autostart=""true"" />" & vbCrLf
                            End If
                        Case "video"
                            'Balise <video> de HTML 5.0 (Standard W3C natif aux navigateurs récents)
                            patternpage &= "<!-- Utilisation de la balise video de HTML5 -->" & vbCrLf & vbCrLf
                            patternpage &= "<video id=""mainplayer"" controls width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autoplay=""true"">" & vbCrLf 'style=""object-fit: fill;""
                            patternpage &= " <source src=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ />" & vbCrLf
                            patternpage &= " <source poster=""" & GetHost() & "thumbnail?t=" & last_view & """ />" & vbCrLf
                            patternpage &= " <p align=center>Votre navigateur ne semble pas prendre en charge la balise video de HTML5.<br><br>Vous pouvez cliquer sur <a href=""/config.cgi"">ce lien</a> pour adapter les paramètres de RetroYT à votre configuration.</p>"
                            patternpage &= "</video>" & vbCrLf
                        Case "realplayer"
                            'Intégration du lecteur Real Player (Le code ci-dessous a été créé par Le Jarb, qui s'est appuyé sur Léo AI. Merci pour son implémentation réussie du plugin Real Player, rendant la lecture intégrée sur navigateur possible sous Windows 3.11/NT 3.51)
                            patternpage &= "<!-- Embarcation du lecteur Real Player 5.0 -->" & vbCrLf & vbCrLf
                            patternpage &= "<embed id=""mainplayer"" src=""" & GetHost() & "v/" & output_filename & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ type=""audio/x-pn-realaudio-plugin"" autostart=""true"" controls=""ImageWindow"" console=""rmplayer"" /><br>" & vbCrLf
                            patternpage &= "<embed width=""" & player_width.ToString & """ height=""20"" type=""audio/x-pn-realaudio-plugin"" controls=""PositionSlider"" console=""rmplayer"" />" & vbCrLf
                            'media_type n'est pas précisé en paramètre, car Real Player ne lit que du RealMedia.
                        Case "activex_realplayer"
                            'Real Player (ActiveX)
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour Real Player 5.0 -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" classid=""CLSID:CFCDAA03-8BE4-11cf-B84B-0020AFBBCCFA"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """>" & vbCrLf
                            patternpage &= " <param name=""src"" value=""" & GetHost() & "v/" & output_filename & """>" & vbCrLf
                            patternpage &= "</object>" & vbCrLf & vbCrLf
                        Case "no_integration"
                            'Aucune intégration, donc aucun lecteur affiché. Code HTML bidon qui suit.
                            patternpage &= "<!-- Aucune intégration activée --><br><br><br><br><br><br><br>" & vbCrLf
                        Case "flash"
                            'Lecteur Flash 8 via Javascript
                            patternpage &= "<!-- Intégration d'un lecteur Flash via Javascript -->" & vbCrLf & vbCrLf
                            patternpage &= "<noscript><p align=center>Javascript et Flash Player 8.0 sont nécessaires pour démarrer la lecture.</p></noscript>" & vbCrLf & vbCrLf
                            patternpage &= "<script language=""javascript"" src=""/swfobject.js""></script>" & vbCrLf
                            patternpage &= "<br>" & vbCrLf
                            patternpage &= "<div id=""mainplayer"" align=""center"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ style=""background-color: black; border-radius: 8px; width: " & player_width.ToString & "px; height: " & player_height.ToString & "px; min-width: 160px; min-height: 120px;""></div>" & vbCrLf & vbCrLf

                            patternpage &= "<script language=""javascript"">" & vbCrLf
                            patternpage &= " var so4 = new SWFObject('/player.swf','mpl','" & player_width.ToString & "','" & player_height.ToString & "','8');" & vbCrLf
                            patternpage &= " so4.addParam('allowscriptaccess','always');" & vbCrLf
                            patternpage &= " so4.addParam('allowfullscreen','true');" & vbCrLf
                            patternpage &= " so4.addVariable('width','" & player_width.ToString & "');" & vbCrLf
                            patternpage &= " so4.addVariable('height','" & player_height.ToString & "');" & vbCrLf
                            patternpage &= " so4.addVariable('file','" & GetHost() & "v/" & output_filename & "');" & vbCrLf
                            patternpage &= " so4.addVariable('searchbar','false');" & vbCrLf
                            patternpage &= " so4.addVariable('linkfromdisplay','true');" & vbCrLf & vbCrLf

                            patternpage &= " so4.write('mainplayer');" & vbCrLf
                            patternpage &= "</script>" & vbCrLf
                            patternpage &= "<br>" & vbCrLf & vbCrLf
                        Case "embed_flash"
                            'Flash via <embed>
                            patternpage &= "<!-- Embarcation directe du lecteur Flash -->" & vbCrLf & vbCrLf
                            patternpage &= "<embed src=""/player.swf"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ id=""mainplayer"" allowfullscreen=""true"" allowscriptaccess=""always"" flashvars=""file=" & GetHost() & "v/" & output_filename & "&searchbar=false&linkfromdisplay=true"" type=""application/x-shockwave-flash"" />" & vbCrLf
                        Case "activex_flash"
                            'Flash via ActiveX
                            patternpage &= "<!-- Intégration d'un objet ActiveX pour le lecteur Flash Player -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" classid=""clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """>" & vbCrLf
                            patternpage &= " <param name=""movie"" value=""/player.swf"">" & vbCrLf
                            patternpage &= " <param name=""allowfullscreen"" value=""true"">" & vbCrLf
                            patternpage &= " <param name=""allowscriptaccess"" value=""always"">" & vbCrLf
                            patternpage &= " <param name=""flashvars"" value=""file=" & GetHost() & "v/" & output_filename & "&searchbar=false&linkfromdisplay=true"">" & vbCrLf
                            patternpage &= " <param name=""wmode"" value=""opaque"">" & vbCrLf
                            patternpage &= "</object>" & vbCrLf & vbCrLf
                        Case "object"
                            'Objet générique sans ActiveX
                            patternpage &= "<!-- Intégration d'un média de façon générique via Object -->" & vbCrLf & vbCrLf
                            patternpage &= "<object id=""mainplayer"" data=""" & GetHost() & "v/" & output_filename & """ src=""" & GetHost() & "v/" & output_filename & """ mrl=""" & GetHost() & "v/" & output_filename & """ target=""" & GetHost() & "v/" & output_filename & """ href=""" & GetHost() & "v/" & output_filename & """ filename=""" & GetHost() & "v/" & output_filename & """ url=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """></object>" & vbCrLf & vbCrLf
                        Case Else
                            'Si par mésaventure, le paramètre manque, affichage d'un lecteur générique.
                            patternpage &= "<!-- Fallback vers une intégration générique via embed -->" & vbCrLf & vbCrLf
                            patternpage &= "<embed id=""mainplayer"" src=""" & GetHost() & "v/" & output_filename & """ mrl=""" & GetHost() & "v/" & output_filename & """ target=""" & GetHost() & "v/" & output_filename & """ href=""" & GetHost() & "v/" & output_filename & """ filename=""" & GetHost() & "v/" & output_filename & """ url=""" & GetHost() & "v/" & output_filename & """ type=""" & media_type & """ width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autostart=""true"" />" & vbCrLf
                            patternpage &= "<p align=center>Mode fallback activé, faute de paramètres interprétables communiqués au serveur.</p><br>"
                    End Select

                    patternpage &= vbCrLf & "<BR>" & vbCrLf

                    'Dans certains cas, le lecteur ne peut pas être disponible, alors on propose tout de même un lien en flux direct, ou pour "forcer" le mode rétrocompatible. Compatibilité garantie sur les très anciens navigateurs.

                    If ultra_legacy Then
                        patternpage &= "<P ALIGN=CENTER>Cliquez <A HREF=""/v/" & output_filename & """ STYLE=""color: " & link_color & ";"">ici</A> pour accéder au flux direct, si la vidéo ne démarre toujours pas.</P>" & vbCrLf
                    Else
                        patternpage &= "<P ALIGN=CENTER>Cliquez <A HREF=""/v/" & output_filename & """ STYLE=""color: " & link_color & ";"">ici</A> pour accéder au flux direct, ou <A HREF=""/watch?v=" & last_view & "&legacy=true"" STYLE=""color: " & link_color & ";"">ici</a> pour forcer le mode rétrocompatibilité.</P>" & vbCrLf
                    End If

                    patternpage &= "</DIV></CENTER><BR><DIV CLASS=""bodysep""></DIV><BR>" & footer & vbCrLf & vbCrLf

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
                    Dim notfound_data As Byte() = GetHTTPBytes(500, "<h1>Error 500 - Internal Server Error</h1>" & vbCrLf & "<p>Proxy server is not connected to the World Wide Web.</p>" & vbCrLf)

                    Try
                        stream.Write(notfound_data, 0, notfound_data.Length)
                    Catch ex As Exception
                        WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                    End Try
                End If
            Else
                'Identifiant invalide manifestement!
                InitValues("Erreur de saisie", , wanted_skin)
                patternpage &= " <P ALIGN=CENTER><BR><B>L'identifiant vidéo que vous avez entré semble invalide. Aucune lecture ne peut être poursuivie.<br><br>Cliquez <A HREF=""/"" STYLE=""color: " & link_color & ";"">ici</A> pour retourner à l'index.</B></P><BR><BR></BODY></HTML>" & vbCrLf & vbCrLf

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
            Dim result_page As String = "<h1>302 Found</h1><p>Please go to this <a href=""/"">link</a> to search a video.</p>" & vbCrLf

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
        ElseIf request.StartsWith("GET /search?q=") Then
            'Lancement d'une recherche par l'utilisateur.

            If IsNetworkAvailable() Then
                Dim arg As String = Split(request)(1)
                arg = arg.Remove(0, 7)

                'Les caractères systèmes sont retirés par sécurité
                For i As Integer = 0 To &H1F
                    request = request.Replace(Chr(i), String.Empty)
                Next

                'Récupérer les 10 vidéos en rapport avec le mot-clef spécifié
                Dim req As String = arg.Remove(0, 3)
                req = UrlDecodeLatin1(req)
                req = req.Replace("+", " ")

                If Not String.IsNullOrEmpty(req) Then
                    WriteLog("Le mot-clef '" & req & "' a été demandé. Recherche en cours...", ConsoleColor.White, client)

                    'Lancement de yt-dlp
                    Dim psi As New ProcessStartInfo()
                    psi.FileName = "yt-dlp.exe"
                    psi.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s<|>%(width)s<|>%(height)s"" ""ytsearch" & number_of_results.ToString & ":" & req & """ --no-warnings --encoding utf-8"

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
                    InitValues("Recherche de " & EscapeHtml(req), req, wanted_skin)

                    'Récupération des lignes
                    Dim lines As String() = output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)

                    If lines.Count = 0 Then
                        'S'il n'y a aucune ligne retournée.
                        patternpage &= " <P ALIGN=CENTER><BR><B><FONT SIZE=4>Aucun résultat trouvé !</FONT></B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & vbCrLf & vbCrLf
                        WriteLog("La recherche du mot-clef '" & req & "' n'a donné aucun résultat.")
                    Else
                        'Sinon, on affiche les résultats dans la page Web.
                        If lines.Count = 1 Then
                            patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Le meilleur résultat pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;» :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                        Else
                            patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Les " & lines.Count.ToString & " meilleurs résultats pour la recherche de «&nbsp;" & EscapeHtml(req) & "&nbsp;» :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                        End If
                        patternpage &= "  <CENTER><TABLE BORDER=0 CELLPADDING=8 WIDTH=780 ALIGN=CENTER>" & vbCrLf

                        WriteLog("La recherche du mot-clef '" & req & "' a donné " & lines.Count.ToString & " résultat(s).")

                        For Each line In lines

                            Dim parts = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                            If parts.Length = 9 Then
                                Dim id As String = parts(0)
                                Dim title As String = parts(1)
                                title = CleanText(title)

                                Dim views As String = parts(2)
                                Dim dateup As String = parts(3)
                                Dim uploader As String = parts(4)
                                Dim thumb As String = "/thumbnail?t=" & id
                                Dim duration As String = parts(6)

                                If Not last_titles.ContainsKey(id) Then
                                    last_titles.Add(id, title)
                                End If

                                If Not video_dimensions.ContainsKey(id) Then
                                    Dim vw, vh As String
                                    vw = parts(7)
                                    vh = parts(8)
                                    If Not IsNumeric(vw) Then vw = 640
                                    If Not IsNumeric(vh) Then vh = 480
                                    video_dimensions.Add(id, vw & ":" & vh)
                                End If

                                Dim legacy_flag As String = String.Empty

                                'Affichage d'une ligne dans les recherches, sous la forme d'une miniature accompagnée de quelques métadonnées.
                                patternpage &= "   <TR>" & vbCrLf
                                patternpage &= "    <TD WIDTH=200><A HREF=""/watch?v=" & id & legacy_flag & """><IMG SRC=""" & thumb & """ WIDTH=180 HEIGHT=120 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A></TD>" & vbCrLf

                                If duration = "NA" Then
                                    patternpage &= "    <TD WIDTH=* VALIGN=TOP><BR><A HREF=""/watch?v=" & id & legacy_flag & """>" & EscapeHtml(title) & " <FONT COLOR=""#808080"">(?:??)</FONT><BR>" & GetThousands(views) & " vue(s)</A><BR><BR>Vidéo publiée le " & GetDate(dateup) & " par <I>" & EscapeHtml(uploader) & "</I>.</TD>" & vbCrLf
                                Else
                                    patternpage &= "    <TD WIDTH=* VALIGN=TOP><BR><A HREF=""/watch?v=" & id & legacy_flag & """>" & EscapeHtml(title) & " <FONT COLOR=""#808080"">(" & GetDuration(CInt(duration)) & ")</FONT><BR>" & GetThousands(views) & " vue(s)</A><BR><BR>Vidéo publiée le " & GetDate(dateup) & " par <I>" & EscapeHtml(uploader) & "</I>.</TD>" & vbCrLf
                                End If

                                patternpage &= "   </TR>" & vbCrLf

                            End If

                        Next

                        patternpage &= "  </TABLE></CENTER>"
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
                    InitValues("Erreur de recherche", , wanted_skin)
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

                client.Close()
            Else
                Dim notfound_data As Byte() = GetHTTPBytes(500, "<h1>Error 500 - Internal Server Error</h1>" & vbCrLf & "<p>Proxy server is not connected to the World Wide Web.</p>" & vbCrLf)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception
                    WriteLog("Erreur lors de l'envoi de la réponse au client: " & ex.Message, ConsoleColor.Red)
                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /search") Then
            'Requête vide
            Dim result_page As String = "<h1>302 Found</h1><p>Please go to this <a href=""/"">link</a> to search a video.</p>" & vbCrLf

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
        ElseIf request.StartsWith("GET /config.cgi") Then
            'Montrer le panneau de configuration client du navigateur
            'message=gotreset, message=gotsaved

            WriteLog("Panneau de configuration demandé par le client.", , client)

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

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

            'Nombre de résultats par recherche et affichage en paramètres
            Select Case number_of_results
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
                Case Else : selected_embed = " SELECTED"
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
                Case Else : selected_framerate24 = " SELECTED"
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
                Case Else : selected_240p = " SELECTED"
            End Select

            Select Case wanted_skin
                Case "oldyt" : selected_classic = " SELECTED"
                Case "cosmic" : selected_cosmic = " SELECTED"
                Case "modern" : selected_modern = " SELECTED"
                Case "dark" : selected_dark = " SELECTED"
                Case "rose" : selected_rose = " SELECTED"
                Case "aqua" : selected_aqua = " SELECTED"
                Case Else : selected_cosmic = " SELECTED"
            End Select

            InitValues("Configuration client", , wanted_skin)
            patternpage &= "<BR><P ALIGN=CENTER><B><FONT SIZE=4>Configuration du client RetroYT :</FONT></B></P><br>" & vbCrLf & vbCrLf

            If request.Contains("message=gotreset") Then
                patternpage &= "<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été remise par défaut avec succès.</FONT></B></P></CENTER><BR>"
            ElseIf request.Contains("message=gotsaved") Then
                patternpage &= "<CENTER><P CLASS=""green_toast""><B><FONT COLOR=""#008000"">La configuration a été enregistrée avec succès.</FONT></B></P></CENTER><BR>"
            End If

            patternpage &= "  <FORM METHOD=""POST"" ACTION=""/savecfg.cgi"">" & vbCrLf
            patternpage &= "   <CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=780>" & vbCrLf
            patternpage &= "    <TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Nombre de résultats affichés par recherche&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""results"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""5""" & selected_five & ">5 résultats</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""10""" & selected_ten & ">10 résultats [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""20""" & selected_twenty & ">20 résultats</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Format vidéo et codec utilisés&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""usedcodec"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_mpeg4""" & selected_avi_mpeg4 & ">AVI (MPEG-4, MP3) [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_msvideo1""" & selected_avi_msvideo1 & ">AVI (MSVideo1, PCM) [Windows 3.11/95/NT]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_yuv""" & selected_avi_yuv & ">AVI (YUV, PCM) [Très lourd!]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""avi_cinepak""" & selected_avi_cinepak & ">AVI (Cinepak, PCM) [Lent à encoder]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_cinepak""" & selected_mov_cinepak & ">MOV (Cinepak, PCM) [MacOS 90s] [Lent]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_mpeg4""" & selected_mov_mpeg4 & ">MOV (MPEG-4, MP2) [MacOS 90s]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_rpza""" & selected_mov_rpza & ">MOV (RPZA, PCM) [MacOS 90s]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""mov_svq1""" & selected_mov_svq1 & ">MOV (Sorenson SVQ1, MP3) [MacOS X 2000s]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mp4""" & selected_mp4 & ">MP4 (H.264, M4A)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mpeg1""" & selected_mpg & ">MPEG (MPEG-1, MP2)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""wmv1""" & selected_oldwmv & ">WMV (WMV1, WMAv1) [Windows 9x/NT]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""wmv2""" & selected_wmv & ">WMV (WMV2, WMAv2) [Windows 98/ME/2000]</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""rm""" & selected_rm & ">Real Media (RV10, Cook)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""3gp""" & selected_3gp & ">3GP (H.263, AMR-NB) [Mobile]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""flv""" & selected_flv & ">Macromedia Flash (Sorenson Spark, MP3)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Résolution de la vidéo&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""usedresolution"" WIDTH=300>" & vbCrLf
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
            patternpage &= "       <OPTION VALUE=""60""" & selected_framerate60 & ">60 images</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Intégration multimédia utilisée&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""usedplayer"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""no_integration""" & selected_nointegration & ">(Aucun lecteur)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""legacy_wmp""" & selected_legacy_wmp & ">Lecteur Windows Media 6.4 (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""wmp""" & selected_wmp & ">Lecteur Windows Media 7.0 et plus (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""quicktime""" & selected_quicktime & ">Lecteur Apple QuickTime (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""embed_quicktime""" & selected_embed_quick & ">Lecteur Apple QuickTime (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""vlc""" & selected_vlc & ">Lecteur VLC (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""alt_vlc""" & selected_altvlc & ">Lecteur VLC (Alternatif)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""embed_vlc""" & selected_vlcembed & ">Lecteur VLC (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""activex_realplayer""" & selected_realplayer_activex & ">Lecteur Real Player (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""realplayer""" & selected_realplayer & ">Lecteur Real Player (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""flash""" & selected_flashplayer & ">Lecteur Flash Player (Javascript)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""embed_flash""" & selected_embedflash & ">Lecteur Flash Player (Embarqué)</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""activex_flash""" & selected_objectflash & ">Lecteur Flash Player (ActiveX)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION DISABLED>------------------------------------</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""embed""" & selected_embed & ">Lecteur embarqué générique [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""object""" & selected_genobject & ">Intégration standard générique</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""video""" & selected_video & ">Vidéo HTML 5.0</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Taille du lecteur multimédia intégré&nbsp;:&nbsp;</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""playersize"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""micro""" & selected_micro & ">Micro (160x140)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""ultrasmall""" & selected_ultrasmall & ">Ultra Compact (256x192)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""small""" & selected_small & ">Compact (320x240)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""middle""" & selected_middle & ">Standard (640x480) [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""large""" & selected_large & ">Large (854x480)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cinema""" & selected_cinema & ">Cinéma (1280x720)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""auto""" & selected_auto & ">Automatique (avec Javascript)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""autoheight""" & selected_auto_height & ">Automatique (avec ratio vidéo)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf & vbCrLf
            patternpage &= "	 <TD ALIGN=RIGHT>Apparence de l'interface Web&nbsp;:&nbsp;</TD>" & vbCrLf & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""skin"" WIDTH=300>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""oldyt""" & selected_classic & ">Classic</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cosmic""" & selected_cosmic & ">Cosmic Tube [Par défaut]</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""modern""" & selected_modern & ">Modern</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""dark""" & selected_dark & ">Dark Mode</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""rose""" & selected_rose & ">Rose</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""aqua""" & selected_aqua & ">Aqua</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf
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

            If String.IsNullOrEmpty(rqcontent) Then
                rqcontent = "results=10&playersize=middle&usedcodec=avi_mpeg4&usedplayer=embed&skin=cosmic&usedresolution=auto&framerate=auto"
            End If

            'Pour éviter des injections d'entêtes HTTP
            If rqcontent.Contains(vbCrLf) Then
                rqcontent = rqcontent.Substring(0, rqcontent.IndexOf(vbCrLf))
            End If

            If rqcontent.Length > 1024 Then 'Limiter le cookie à 1Ko
                rqcontent = rqcontent.Substring(0, 1024)
            End If

            Dim result_page As String = "<h1>302 Found</h1><p>Configuration has been saved, you can now navigate to <a href=""/config.cgi"">this page</a>.</p>" & vbCrLf

            Dim index_resp As String =
                "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Set-Cookie: " & cookie_header & rqcontent & ";path=/" & vbCrLf & 'L'ajout de la variable path garantit l'usage du cookie sur tout le domaine (compatibilité IE6 et assimilés)
                "Location: /config.cgi?message=gotsaved" & vbCrLf &
                "Accept-Ranges: bytes" & vbCrLf & vbCrLf & result_page 'Petit message avec lien si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception
                WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
                client.Close()
                Exit Sub
            End Try

            WriteLog("Nouveau cookie pour le client: " & rqcontent, ConsoleColor.Yellow, client)
            client.Close()

        ElseIf request.Contains("GET /savecfg.cgi") Then
            'Message d'erreur requête vide
            WriteLog("Erreur 400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<h1>Error 400 - Bad Request</h1>" & vbCrLf & "<p>HTTP request is empty. Please use the <a href=""/config.cgi"">parameters section</a> to change the client settings.</p>" & vbCrLf)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception
                WriteLog("Erreur d'envoi de la réponse: " & ex.Message, ConsoleColor.Red, client)
            End Try

            client.Close()

        ElseIf request.StartsWith("GET /resetcfg.cgi") Then
            'Réinitialiser la configuration client

            Dim result_page As String = "<h1>302 Found</h1><p>Configuration has been reset, you can now navigate to <a href=""/config.cgi"">this page</a>.</p>" & vbCrLf

            Dim index_resp As String =
                "HTTP/" & http_ver & " 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Set-Cookie: " & cookie_header & "results=10&playersize=middle&usedcodec=avi_mpeg4&usedplayer=embed&skin=cosmic&usedresolution=auto&framerate=auto;path=/" & vbCrLf &
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

            If Not IO.File.Exists(CurDir() & "\vidcache\" & arg1) Then
                Dim notfound_data As Byte()
                notfound_data = GetHTTPBytes(404, "<h1>Error 404 - Not Found</h1>" & vbCrLf & "<p>Video with file name '" & arg1.Replace(">", "&gt;").Replace("<", "&lt;") & "' was not found on this server.</p>" & vbCrLf)

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
                    Case "avi_msvideo1", "avi_mpeg4", "avi_yuv", "avi_cinepak" : media_type = "video/x-msvideo"
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
                        Dim invalidrangedata As Byte() = GetHTTPBytes(416, "<h1>Error 416 - Range Not Satisfiable</h1>" & vbCrLf & "<p>La requête envoyée par le navigateur est erronée, car les offsets demandés dans le fichier sont invalides.</p>" & vbCrLf)

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
                    percent = CInt((total_sent / fs_output.Length) * 100)
                    WriteLog("Transfert du fichier vidéo '" & arg1 & "' interrompu à " & percent & "%, car: " & ex.Message, ConsoleColor.Yellow, client)
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
            InitValues("À propos de RetroYT", , wanted_skin)
            WriteLog("Page des informations sur le logiciel envoyée.", , client)

            patternpage &= "<br><br><center><div style=""display: block; width: 780px; margin-left: auto; margin-right: auto; text-align: left; text-align: justify;""><B>RetroYT</B> est un proxy multimédia pour YouTube développé en Visual Basic .NET 2022 par Monokeros. La version actuelle, la Bêta 3.3, a été publiée le 8 mai 2026. Ce projet est distribué gratuitement (sous la licence «&nbsp;freeware&nbsp;»), sans aucune garantie explicite ou implicite. L'auteur ne pourra être tenu responsable d'éventuels dommages matériels, logiciels, des éventuelles pertes de données, ou dysfonctionnements résultant de son utilisation, y compris dans un cadre normal.<br>" & vbCrLf
            patternpage &= "Le projet vise principalement à restaurer la compatibilité de YouTube avec des systèmes d'exploitation, navigateurs web et lecteurs multimédia anciens ou obsolètes, à travers le relais de connexions, formatage vers un code HTML, et l'intégration de formats vidéo historiques, lisible par les navigateurs de toute époque.<br><br>" & vbCrLf
            patternpage &= "Remerciements à LeJarb pour son aide concernant l'optimisation de certains codecs, ainsi que pour son implémentation de la lecture intégrée via RealPlayer (avec l'assistance de Léo AI).<br><br><br>" & vbCrLf

            patternpage &= "<div style=""border: 1px solid black; padding: 8px 8px 8px 8px; width: 40%;""><b>Sommaire: </b><br>" & vbCrLf
            patternpage &= "<a href=""#introduction"" style=""color: " & link_color & ";"">I. Introduction</a><br>" & vbCrLf
            patternpage &= "<a href=""#parameters"" style=""color: " & link_color & ";"">II. Paramètres</a><br>" & vbCrLf
            patternpage &= "<a href=""#precautions"" style=""color: " & link_color & ";"">III. Précautions</a><br>" & vbCrLf
            patternpage &= "<a href=""#configuration"" style=""color: " & link_color & ";"">IV. Configuration</a><br>" & vbCrLf
            patternpage &= "<a href=""#useget"" style=""color: " & link_color & ";"">V. Utilisation du paramètre GET</a><br>" & vbCrLf
            patternpage &= "<a href=""#credits"" style=""color: " & link_color & ";"">VI. Crédits</a></div><br><br>" & vbCrLf & vbCrLf

            patternpage &= "<center><h2><a name=""introduction"">I. Introduction</a></h2></center><br><br>" & vbCrLf
            patternpage &= "Le nom «&nbsp;RetroYT&nbsp;» provient du terme «&nbsp;rétro&nbsp;», désignant de manière générale quelque chose d'ancien, de classique ou «&nbsp;à l'ancienne&nbsp;». Le logiciel repose sur un serveur Web codé directement dans l'application (dit «&nbsp;hardcodé&nbsp;»), servant d'intermédiaire entre YouTube et le navigateur client utilisé par l'utilisateur. L'objectif principal du projet est de restaurer un accès fonctionnel à YouTube sur des navigateurs et systèmes d'exploitation devenus trop anciens pour prendre en charge la version moderne du site. Bien que RetroYT puisse également être utilisé depuis un navigateur récent comme un proxy classique, ce n'est pas sa vocation première. De nombreux proxies YouTube modernes existent déjà et offrent généralement de meilleures performances et une compatibilité plus étendue avec les standards Web actuels.<BR>RetroYT vise avant tout à permettre la recherche et la lecture de vidéos YouTube depuis des environnements anciens ou obsolètes, tels que Windows 3.11, Windows 95, Windows 98, Windows NT 4.0, Windows 2000, certaines anciennes versions de Mac OS X, ainsi que divers systèmes UNIX/Linux historiques. La solution a également été testée sous Windows XP et Windows 11 avec succès. Il est donc parfaitement normal de retrouver, au sein de ce projet, du code HTML volontairement ancien, des méthodes d'intégration multimédia historiques, ou encore l'utilisation de technologies aujourd'hui abandonnées comme ActiveX, RealPlayer, des anciennes versions de QuickTime, Flash Player, ou les plugins NPAPI. L'ensemble du projet cherche à reproduire, autant que possible, une expérience cohérente avec les capacités techniques du Web des années 1990 et du début des années 2000, tout en offrant une expérience de navigation proche des services Internet actuels.<br><br>" & vbCrLf

            patternpage &= "<br><center><h2><a name=""parameters"">II. Paramètres</a></h2></center><br><br>" & vbCrLf

            patternpage &= "<b>RetroYT</b> propose un ensemble de paramètres permettant d'adapter le fonctionnement du proxy aux capacités matérielles et logicielles du système cible. L'utilisateur peut notamment choisir la taille du lecteur vidéo, le format et les codecs employés pour la conversion, ainsi que le nombre d'images par seconde. Pour les systèmes les plus anciens, comme Windows 95 ou Windows NT 4.0, l'utilisation du codec MSVideo1 (Microsoft Video 1) est fortement recommandée, en raison de son excellente compatibilité avec les anciennes versions de Windows. Comme beaucoup de codecs historiques, celui-ci produit toutefois des fichiers relativement volumineux, en particulier pour les vidéos dépassant plusieurs minutes.<BR>Selon la puissance de votre machine, votre quantité de mémoire disponible ou la vitesse de votre connexion réseau, le transfert et la lecture des vidéos peuvent devenir plus difficiles. La résolution vidéo peut être choisie parmi un certain nombre de valeurs prédéfinies (96p, 120p, 144p, 240p, 360p, 480p, 720p et 1080p), ou laissée en mode automatique afin que le serveur sélectionne lui-même le format le plus approprié. Certains codecs anciens possèdent volontairement des limitations de résolution ou de format d'image, principalement pour des raisons de compatibilité avec les anciens lecteurs multimédia ou les contraintes matérielles des systèmes ciblés.<br><br>" & vbCrLf & vbCrLf
            patternpage &= "Le mode d'intégration du lecteur vidéo est également configurable. RetroYT peut utiliser différentes méthodes historiques de lecture multimédia, parmi lesquelles :" & vbCrLf & vbCrLf

            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li>L'intégration ActiveX de Windows Media Player 6.4 ou supérieur ;</li>" & vbCrLf
            patternpage &= " <li>La balise HTML embed ;</li>" & vbCrLf
            patternpage &= " <li>L'intégration de lecteurs externes tels que VLC, QuickTime ou RealPlayer ;</li>" & vbCrLf
            patternpage &= " <li>Le fameux lecteur Flash Player, très utilisé aux grands débuts de YouTube ;</li>" & vbCrLf
            patternpage &= " <li>Ou encore la balise video, sous navigateurs modernes compatibles HTML5 (sortis après 2008).</li>" & vbCrLf
            patternpage &= "</ul>" & vbCrLf

            patternpage &= "L'apparence générale de l'interface Web peut également être personnalisée grâce à plusieurs thèmes graphiques :" & vbCrLf & vbCrLf

            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li><b>Classic :</b> Interface inspirée du site de YouTube des années 2000 ;</li>" & vbCrLf
            patternpage &= " <li><b>Cosmic :</b> Reproduction fidèle du thème «&nbsp;Cosmic Panda&nbsp;» utilisé officiellement entre 2011 et 2013 sur ce même site ;</li>" & vbCrLf
            patternpage &= " <li><b>Modern :</b> Interface proche du YouTube actuel ;</li>" & vbCrLf
            patternpage &= " <li><b>Dark Mode :</b> Affichage clair sur fond sombre ;</li>" & vbCrLf
            patternpage &= " <li><b>Rose :</b> Thème aux couleurs douces, rappelant certaines interfaces Web des années 1990 ;</li>" & vbCrLf
            patternpage &= " <li><b>Aqua :</b> Thème aux couleurs bleues, rappelant l'eau. Le seul thème qui n'embarque pas de rouge partout, un peu comme le mode sombre.</li>" & vbCrLf
            patternpage &= "</ul>" & vbCrLf & vbCrLf

            patternpage &= "Ces options permettent d'adapter RetroYT aussi bien à des machines très anciennes qu'à des systèmes plus récents, tout en conservant une esthétique cohérente avec les différentes époques du Web.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><center><h2><a name=""precautions"">III. Précautions</a></h2></center><br><br>" & vbCrLf
            patternpage &= "<B>RetroYT</B> est distribué sous licence freeware/open source et ne doit pas être revendu sans l'autorisation explicite de son auteur. Afin de conserver une compatibilité maximale avec les anciens navigateurs Web et systèmes d'exploitation, le proxy ne met volontairement pas en œuvre certaines technologies modernes de sécurisation des communications, notamment SSL/TLS côté client. Les échanges entre RetroYT et YouTube utilisent bien des connexions sécurisées modernes, mais les communications entre le client et le proxy restent, quant à elles, entièrement non chiffrées. En effet, nombre d'anciens navigateurs ne prennent pas en charge SSL/TLS, surtout dans leurs dernières versions. Le HTTP sans chiffrement est une solution universelle pour se connecter au serveur.<BR>Pour cette raison, RetroYT est principalement destiné à une utilisation au sein d'un réseau local (LAN), sur une machine personnelle ou dans un environnement contrôlé. Il est fortement déconseillé d'exposer directement le proxy sur Internet ou de l'utiliser sur un réseau public non sécurisé, sauf si vous utilisez des solutions complémentaires de protection telles qu'un VPN ou un tunnel sécurisé.<BR><BR>"
            patternpage &= "RetroYT utilise également un système de cache local afin d'améliorer les performances et limiter les téléchargements répétés. Deux dossiers principaux sont utilisés :" & vbCrLf & vbCrLf
            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li>Le dossier <i>thumbs</i> : Stockage des miniatures YouTube (Qualité moyenne, alias MQ) envoyées au client à la demande ;</li>" & vbCrLf
            patternpage &= " <li>Le dossier <i>vidcache</i> : Stockage des vidéos converties et mises en cache pour être envoyées au client.</li>" & vbCrLf
            patternpage &= "</ul>" & vbCrLf & vbCrLf

            patternpage &= "Ces dossiers peuvent être vidés manuellement si l'espace disque disponible devient insuffisant. Normalement, le logiciel gère lui-même la taille du cache et/ou le nombre de fichiers. Le dossier <i>srvlogs</i> contient tous les fichiers de rapport de connexion et des actions du serveur, avec heure et date. Bien que ces fichiers soient facultatifs et aisément supprimables, en revanche, certains fichiers et répertoires sont indispensables au fonctionnement du logiciel et ne doivent pas être supprimés :" & vbCrLf & vbCrLf

            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li>Le dossier <i>resfiles</i>, qui contient les ressources du projet, comme les images du site Web interne ;</li>" & vbCrLf
            patternpage &= " <li>Le dossier <i>flplayer</i>, qui contient les fichiers du lecteur Flash Player, au cas où il serait activé ;</li>" & vbCrLf
            patternpage &= " <li>Les fichiers <i>YTSrv.deps.json</i>, et <i>YTSrv.runtimeconfig.json</i> qui sont des scripts json vitaux pour que les binaires fonctionnent ;</li>" & vbCrLf
            patternpage &= " <li>Les fichiers <i>YTSrv.dll</i> et <i>YTSrv.pdb</i>, générés par Visual Basic .NET et indispensables au fonctionnement du logiciel ;</li>" & vbCrLf
            patternpage &= " <li><i>ffmpeg.exe</i> mis par les soins de l'utilisateur. Il permet de convertir à la volée les fichiers vidéo téléchargés au format MP4 ;</li>" & vbCrLf
            patternpage &= " <li><i>yt-dlp.exe</i> mis par les soins de l'utilisateur également. Il permet d'obtenir des vidéos depuis YouTube ;</li>" & vbCrLf
            patternpage &= " <li>RetroYT.exe qui est le fichier binaire de lancement du logiciel lui-même.</li>" & vbCrLf
            patternpage &= "</ul>" & vbCrLf & vbCrLf

            patternpage &= "La suppression de ces éléments empêcherait le démarrage ou le fonctionnement correct du proxy. Si le serveur est fermé pendant la conversion d'un fichier vidéo, un fichier temporaire nommé <i>output.lock</i> est généré. Au cas où vous redémarreriez le logiciel, ce fichier contient l'identifiant du processus ffmpeg.exe dernièrement lancé, ainsi que le fichier qui était en cours de traitement. Ainsi, le processus fantôme de ffmpeg sera coupé, le fichier temporaire sera supprimé, ainsi que le fichier vidéo dont la conversion a été inaccomplie, pour éviter tout fichier corrompu et tout plantage.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><center><h2><a name=""configuration"">IV. Configuration</a></h2></center><br><br>" & vbCrLf
            patternpage &= "Du côté du serveur, il est recommandé d'exécuter RetroYT sur une machine relativement performante. Une connexion Internet stable et rapide est également recommandé. Le transcodage vidéo effectué par FFmpeg peut solliciter fortement le processeur, en particulier lors de l'utilisation de codecs anciens ou peu optimisés comme Cinepak ou MSVideo1. Windows 10 et Windows 11 sont actuellement les systèmes les plus recommandés pour héberger le proxy. Le logiciel nécessite l'environnement .NET 6.0 ou plus, afin de fonctionner correctement. Du côté client, RetroYT a été conçu pour rester accessible à des navigateurs et systèmes beaucoup plus anciens. La navigation sur le proxy ainsi que la lecture vidéo intégrée ont notamment été testées avec succès sur les configurations suivantes :<br><br>" & vbCrLf & vbCrLf
            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li>Windows NT 4.0 SP6 / Internet Explorer 5.5 / Windows Media Player 6.4 / 1Go de RAM, 32Mo de mém. vidéo et proc. de 700MHz ;</li>" & vbCrLf
            patternpage &= " <li>Windows 2000 SP4 / Internet Explorer 6.0 / Windows Media Player 9.0 / 3Go de RAM, 256Mo de mém. vidéo et proc. de 1,85GHz ;</li>" & vbCrLf
            patternpage &= " <li>Windows XP / Internet Explorer 6.0 / Windows Media Player 11.0 / 2Go de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows XP / Firefox 52.0 / Plugin de VLC Media Player 3.0 / 2Go de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows ME / Internet Explorer 5.5 / Windows Media Player 7.0 / 1Go de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows 98 SE / Internet Explorer 4.01 / Flash Player 8 / 1Go de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows 3.11 / Internet Explorer 4.01 / Real Player 5.0 / 64Mo de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows NT 3.51 / Internet Explorer 4.01 / Real Player 5.0 / 64Mo de RAM ;</li>" & vbCrLf
            patternpage &= " <li>MacOS X 7.5.3 / NetScape 1.1 et Internet Explorer 4.01 / Apple QuickTime 3 / 512Mo de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Linux CentOS 6.10 / SeaMonkey 2.49.7 / Totem et GStreamer / 2Go de RAM ;</li>" & vbCrLf
            patternpage &= " <li>Windows 11 / Opera 130.0 / Intégration vidéo HTML5 / 16Go de RAM, 2,8GHz de processeur, et 6Go de mémoire vidéo.</li>" & vbCrLf
            patternpage &= "</ul><br>" & vbCrLf & vbCrLf

            patternpage &= "Veillez à autoriser l'exécution des contrôles ActiveX, si vous utilisez un système d'exploitation de Microsoft. Veillez aussi à avoir un ou plusieurs lecteurs multimédias installés, et les cookies activés sur votre navigateur. Si ce dernier ne semble pas prendre en charge les cookies, vous pourrez toujours forcer le mode rétrocompatibilité sur la page de visualisation, en cliquant sur le lien intitulé «&nbsp;Forcer le mode rétrocompatibilité&nbsp;». Pour les très anciennes versions de Windows, faire usage du codec AVI MSVideo1 depuis la section ""Paramètres"" est recommandé, en résolution 240p et en 15 images/s, tout en veillant à ce que les vidéos ne dépassent pas 10 minutes de longueur. Il s'agit d'un codec avec compression intégrée, totalement compatible avec Windows depuis sa version 3.1. Pour les navigateurs compatibles HTML5, vous pouvez activer l'utilisation du format vidéo MP4, et l'intégration multimédia via la balise video.<br>" & vbCrLf
            patternpage &= "Si vous activez le lecteur Flash Player, seul le format FLV (Flash Video) pourra être lu. Pareil pour Real Player, seul le format Real Media sera lu. Si par malheur aucune de ces options ne fonctionne, vous pouvez également cliquer sur le lien pour lire le flux vidéo directement (lien présent sous le lecteur, si présent). Le navigateur ouvrira un lecteur externe, ou vous proposera de télécharger le fichier pour le lire après. Mais il s'agit d'une option de dernier recours. Concernant le lecteur Windows Media Player 6.4, notez bien que l'utilisation des URL n'est prise en charge qu'à partir de la version 6.4.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><center><h2><a name=""credits"">V. Utilisation du paramètre GET</a></h2></center><br><br>" & vbCrLf

            patternpage &= "Si les cookies ne fonctionnent pas sur votre navigateur, et que vous ne pouvez pas enregistrer les paramètres, ceux par défaut seront appliqués. Par conséquent, certaines fonctionnalités seront incompatibles avec votre configuration. Heureusement, RetroYT inclut une fonctionnalité pour remédier à cet éventuel manque. Pour modifier la configuration de la lecture sans passer par les cookies (et la sauvegarde du paramétrage qui utilise une requête POST), vous pouvez ajouter des paramètres GET dans l'URL qui suit le modèle <i>/watch?v=xxxxxxxxxxx</i>. Ce sont les mêmes attributs que ceux utilisés dans la requête POST. Vous pouvez changer le type de lecteur utilisé, la taille du lecteur, le format vidéo utilisé, le nombre d'images par seconde et la résolution.<br><br>" & vbCrLf

            patternpage &= "<b>Le lecteur utilisé se change via l'entête <i>usedplayer</i> avec pour paramètre un des éléments suivants :</b><br><br>"
            patternpage &= "no_integration = Aucune intégration<br>" & vbCrLf
            patternpage &= "legacy_wmp = Lecteur Windows Media Player 6.4<br>" & vbCrLf
            patternpage &= "wmp = Lecteur Windows Media Player 7.0<br>" & vbCrLf
            patternpage &= "embed = Intégration par la balise HTML embed<br>" & vbCrLf
            patternpage &= "video = Intégration avec la balise vidéo de HTML5<br>" & vbCrLf
            patternpage &= "realplayer = Lecteur Real Player via embed<br>" & vbCrLf
            patternpage &= "activex_realplayer = Lecteur Real Player via ActiveX<br>" & vbCrLf
            patternpage &= "embed_vlc = Lecteur VLC via embed<br>" & vbCrLf
            patternpage &= "vlc = Lecteur VLC via ActiveX<br>" & vbCrLf
            patternpage &= "alt_vlc = Lecteur VLC via ActiveX (Alternatif)<br>" & vbCrLf
            patternpage &= "quicktime = Lecteur QuickTime via ActiveX<br>" & vbCrLf
            patternpage &= "embed_quicktime = Lecteur QuickTime via embed<br>" & vbCrLf
            patternpage &= "flash = Lecteur Flash via Javascript<br>" & vbCrLf
            patternpage &= "embed_flash = Lecteur Flash via embed<br>" & vbCrLf
            patternpage &= "activex_flash = Lecteur Flash via ActiveX<br>" & vbCrLf
            patternpage &= "object = Intégration via la balise HTML object<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<b>La taille du lecteur utilisée se règle avec l'entête <i>playersize</i>, suivi par un des paramètres suivants :</b><br><br>" & vbCrLf
            patternpage &= "auto = Taille gérée par Javascript (Il doit être disponible et activé)<br>" & vbCrLf
            patternpage &= "micro = Taille 160x120 pixels (Pour les écrans de portables)<br>" & vbCrLf
            patternpage &= "ultrasmall = Taille 256x192 pixels<br>" & vbCrLf
            patternpage &= "small = Taille 320x240 pixels (Pour les écrans VGA de base)<br>" & vbCrLf
            patternpage &= "middle = Taille 640x480 pixels (Taille par défaut)<br>" & vbCrLf
            patternpage &= "large = Taille 854x480 pixels (Format large minimal)<br>" & vbCrLf
            patternpage &= "cinema = Taille 1280x800 pixels (Format large standard)<br>" & vbCrLf
            patternpage &= "autoheight = Taille du lecteur basée sur la taille de la vidéo<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<b>Le format vidéo utilisé pour la lecture a pour entête <i>usedcodec</i>, et est accompagné d'un des paramètres suivants :</b><br><br>" & vbCrLf
            patternpage &= "mpeg1 = Format MPEG, codec vidéo MPEG-1, codec audio MP2<br>" & vbCrLf
            patternpage &= "avi_mpeg4 = Format AVI (Microsoft), codec vidéo MPEG-4, codec audio MP3<br>" & vbCrLf
            patternpage &= "avi_msvideo1 = Format AVI (Microsoft), codec vidéo MSVideo1, codec audio PCM<br>" & vbCrLf
            patternpage &= "avi_cinepak = Format AVI (Microsoft), codec vidéo Cinepak, codec audio PCM<br>" & vbCrLf
            patternpage &= "avi_yuv = Format AVI (Microsoft), vidéo en YUV, codec audio PCM<br>" & vbCrLf
            patternpage &= "mp4 = Format MP4, codec vidéo H.264, codec audio M4A<br>" & vbCrLf
            patternpage &= "wmv1 = Format WMV, codec vidéo WMV1, codec audio WMAv1<br>" & vbCrLf
            patternpage &= "wmv2 = Format WMV, codec vidéo WMV2, codec audio WMAv2<br>" & vbCrLf
            patternpage &= "mov_cinepak = Format Apple QuickTime (MOV), codec vidéo Cinepak, codec audio PCM<br>" & vbCrLf
            patternpage &= "mov_svq1 = Format Apple QuickTime (MOV), codec vidéo Sorenson SVQ1, codec audio MP3<br>" & vbCrLf
            patternpage &= "mov_mpeg4 = Format Apple QuickTime (MOV), codec vidéo MPEG-4, codec audio MP3<br>" & vbCrLf
            patternpage &= "mov_rpza = Format Apple QuickTime (MOV), codec vidéo RPZA, codec audio PCM<br>" & vbCrLf
            patternpage &= "3gp = Format 3GP (3G Video), codec vidéo H.263, codec audio AMR Narrowband<br>" & vbCrLf
            patternpage &= "flv = Format Flash Video, codec vidéo Sorenson Spark, codec audio MP3<br>" & vbCrLf
            patternpage &= "rm = Format Real Media, codec vidéo RV10, codec audio AC3<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<b>Le nombre d'images est paramétré avec l'entête <i>framerate</i> suivi du nombre d'images voulues parmi cette liste :</b><br><br>" & vbCrLf
            patternpage &= "auto = Meilleur nombre d'images par seconde pour un format voulu<br>" & vbCrLf
            patternpage &= "10 = 10 images par seconde (pour vieux ordinateurs)<br>" & vbCrLf
            patternpage &= "12 = 12 images par seconde<br>" & vbCrLf
            patternpage &= "15 = 15 images par seconde (Bon rapport qualité/quantité pour les vieux ordinateurs)<br>" & vbCrLf
            patternpage &= "20 = 20 images par seconde<br>" & vbCrLf
            patternpage &= "24 = 24 images par seconde [Par défaut]<br>" & vbCrLf
            patternpage &= "25 = 25 images par seconde<br>" & vbCrLf
            patternpage &= "30 = 30 images par seconde<br>" & vbCrLf
            patternpage &= "60 = 60 images par seconde (Vivement déconseillé sur les anciens PC)<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<b>La résolution de la vidéo, intitulée <i>usedresolution</i>, peut être choisie parmi les paramètres suivants :</b><br><br>" & vbCrLf
            patternpage &= "auto = Meilleure résolution choisie par le serveur, pour chaque format voulu<br>" & vbCrLf
            patternpage &= "96p = Résolution minimale, surtout utile pour le format 3GP<br>" & vbCrLf
            patternpage &= "120p = Résolution très faible<br>" & vbCrLf
            patternpage &= "144p = Résolution faible<br>" & vbCrLf
            patternpage &= "240p = Petite résolution<br>" & vbCrLf
            patternpage &= "360p = Moyenne résolution<br>" & vbCrLf
            patternpage &= "480p = Résolution standard<br>" & vbCrLf
            patternpage &= "720p = Haute résolution<br>" & vbCrLf
            patternpage &= "1080p = Très haute résolution (Pour les PC de la génération de Windows Vista et plus)<br><br>" & vbCrLf & vbCrLf

            patternpage &= "Sacrées listes, n'est-ce pas? Certaines résolutions seront indisponibles sous certains formats. Pareil pour le nombre d'images. Les paramètres erronés ou inexistants seront ignorés. Pour illustrer un usage concret de cette fonctionnalité :<br><br>" & vbCrLf & vbCrLf
            patternpage &= "http://127.0.0.1/watch?v=dQw4w9WgXcQ?usedplayer=video&playersize=auto&usedcodec=mp4&framerate=24&usedresolution=480p<br><br>"
            patternpage &= "Ceci démarrera la lecture de la vidéo au format MP4 (Résolution 480p @ 24 FPS), via le lecteur vidéo intégré de HTML5. La taille du lecteur sera automatiquement réglée.<br><br>"
            patternpage &= "Tous les paramètres ne sont pas obligatoires. Ainsi, pour démarrer, par exemple, une lecture avec le lecteur Flash :<br><br>"
            patternpage &= "http://127.0.0.1/watch?v=ZyhrYis509A?usedplayer=flash&usedcodec=flv&usedresolution=240p<br><br>"
            patternpage &= "Le tout en 240p, avec le nombre d'images par seconde par défaut. Le reste des paramètres utiliseront ceux par défaut également. Cette configuration reste assez typique de l'époque de Flash Player, dans les années 2000.<br>" & vbCrLf
            patternpage &= "Si vous lisez depuis Windows 3.11 ou Windows NT 3.51, je vous conseille d'installer Real Player 5.0, qui rendra possible la lecture sous Internet Explorer 4 ou 5. Les paramètres à utiliser seront ainsi :<br><br>" & vbCrLf
            patternpage &= "http://127.0.0.1/watch?v=FuOhQZP821o?usedplayer=realplayer&usedcodec=rm&usedresolution=240p&framerate=15<br><br>" & vbCrLf
            patternpage &= "Ce ne sont que des exemples, mais ils vous inspireront probablement pour votre configuration. Faites-en bon usage.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><center><h2><a name=""credits"">VI. Crédits</a></h2></center><br><br>" & vbCrLf
            patternpage &= "YouTube est une propriété de Google. Il s'agit d'une plateforme de diffusion de vidéos en direct, ou en différé. Ce projet de proxy n'est pas affilié à Google, ni à YouTube." & vbCrLf
            patternpage &= "Ce logiciel a été développé sous Microsoft Visual Basic .NET 2022. Il fait usage des librairies et binaires ffmpeg, et du projet yt-dlp, que l'utilisateur doit intégrer manuellement au dossier (ils ne sont pas livrés par défaut pour éviter des conflits d'intérêt avec leurs auteurs respectifs, et pour des raisons d'espace disque).<BR>Merci à ChatGPT pour ses astuces de programmation. Sans lui, ce projet n'aurait peut-être jamais vu le jour. Je remercie aussi LeJarb pour le code d'intégration de Real Player, et son optimisation de l'usage des codecs (en s'aidant de Léo AI). Je le remercie aussi pour ses divers feedbacks, et sa participation active dans l'amélioration du projet. Je remercie aussi Val pour ses tests du logiciel sur des configurations réelles. Merci également à vous, l'utilisateur, pour avoir utilisé RetroYT, en espérant qu'il fonctionnera parfaitement sur votre configuration, et qu'il vous procurera entière satisfaction dans l'usage du service YouTube depuis d'anciens systèmes.<br><br><i>L'auteur.</i><br><br>" & vbCrLf & vbCrLf
            patternpage &= "<A HREF=""/"" STYLE=""color: " & link_color & ";"">Cliquez ici pour retourner à l'index</A><BR><BR>" & vbCrLf
            patternpage &= "</div></center><div class=bodysep></div>" & footer

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
                InitValues("Accueil", , wanted_skin)
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
                    Case "yt_logo2.gif", "yt_logo.gif", "yt_modrn.gif", "yt_dark.gif", "yt_rose.gif", "yt_aqua.gif", "cosmic.gif"
                        'Les logos RetroYT, qui font penser à ceux de YouTube, sont mis au format GIF pour garantir une compatibilité maximale avec les navigateurs anciens.
                        'Aussi cosmic.gif.
                        sent_res &= "Content-Type: image/gif" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Accept-Ranges: bytes" & vbCrLf
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
                        sent_css &= " line-height: 18px;"
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
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= ".thumbstyle {" & vbCrLf
                        sent_css &= " border-radius: 4px;" & vbCrLf
                        sent_css &= " width: 180px;" & vbCrLf
                        sent_css &= " height: 120px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a {" & vbCrLf

                        If wanted_skin = "dark" Then
                            sent_css &= " color: white;" & vbCrLf
                        Else
                            sent_css &= " color: black;" & vbCrLf
                        End If

                        sent_css &= " font-weight: bold;" & vbCrLf
                        sent_css &= " text-decoration: none;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "a:hover {" & vbCrLf
                        sent_css &= " text-decoration: underline;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "#mainplayer {" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= " border-radius: 8px;" & vbCrLf
                        sent_css &= " display: block;" & vbCrLf
                        sent_css &= " object-fit: center;" & vbCrLf
                        sent_css &= " margin-left: auto;" & vbCrLf
                        sent_css &= " margin-right: auto;" & vbCrLf
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
                            Case Else
                                sent_css &= " background-color: #e01425;" & vbCrLf
                                sent_css &= " background-image: url('btn_grad.png');" & vbCrLf
                        End Select

                        sent_css &= " background-repeat: repeat-x;" & vbCrLf
                        sent_css &= " font-weight: bold;" & vbCrLf
                        sent_css &= " cursor: hand;" & vbCrLf
                        sent_css &= " cursor: pointer;" & vbCrLf
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

                        Dim notfound_data As Byte() = GetHTTPBytes(404, "<h1>Error 404 - Not Found</h1>" & vbCrLf & "<p>Resource '<i>/" & arg & "</i>' was not found on this server.</p>" & vbCrLf)

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
            WriteLog("Erreur 400: Requête erronée envoyée.", , client)

            Dim baddata As Byte() = GetHTTPBytes(400, "<h1>Error 400 - Bad Request</h1>" & vbCrLf & "<p>Invalid or malformed HTTP request.</p>" & vbCrLf)

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
