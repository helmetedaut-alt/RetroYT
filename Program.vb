Imports System.Net
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Module Program

    'Projet RetroYT codé par Monokeros en 2026
    'Tous droits réservés. Licence freeware/open source.

    Public finalpath As String = CurDir() & "\output.mp4" 'L'endroit où télécharger le fichier MP4 de la vidéo voulue.
    Public port As Integer = 80 'Port à écouter pour créer le serveur
    Public patternpage As String = Nothing 'Page HTML modèle à renvoyer au client
    Public last_titles As New Dictionary(Of String, String) 'Cache des vidéos lues/recherchées avec leur titre
    Public last_view As String = Nothing 'Identifiant de la vidéo en cours de lecture
    Public iso As Encoding = Encoding.GetEncoding("iso-8859-1")

    'Pied de page générique à certaines pages.
    Public Const footer As String = "<HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & "<P ALIGN=CENTER><B>RetroYT</B> - Copyright &copy; 2026, tous droits réservés. YouTube est une propriété de Google.<BR>Ce projet n'est pas affilié avec cette entreprise. <A HREF=""/about"" STYLE=""color: darkred;"">Plus d'informations sur RetroYT</A>.</P>" & vbCrLf & "</BODY>" & vbCrLf & "</HTML>" & vbCrLf

    Sub InitValues(Optional ByVal t As String = Nothing, Optional ByVal k As String = Nothing, Optional ByVal skin As String = "cosmic")
        'Cette fonction génère une entête et un corps de page HTML à retourner au client.
        patternpage = "<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"" ""https://www.w3.org/TR/html4/loose.dtd"">" & vbCrLf
        patternpage &= "<HTML>" & vbCrLf
        patternpage &= " <HEAD>" & vbCrLf

        If t = Nothing Then
            patternpage &= "  <TITLE>RetroYT</TITLE>" & vbCrLf
        Else
            'Echappement des caractères pour éviter les bugs et les injections HTML.
            t = t.Replace("<", "&gt;")
            t = t.Replace(">", "&lt;")
            patternpage &= "  <TITLE>RetroYT - " & t & "</TITLE>" & vbCrLf
        End If

        patternpage &= "  <META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=iso-8859-1"">" & vbCrLf
        patternpage &= "  <META CHARSET=""iso-8859-1"" />" & vbCrLf
        patternpage &= "  <LINK REL=""shortcut icon"" HREF=""/favicon.ico"" />" & vbCrLf
        patternpage &= "  <LINK REL=""stylesheet"" TYPE=""text/css"" HREF=""/style.css"" />" & vbCrLf
        patternpage &= " </HEAD>" & vbCrLf & vbCrLf

        If skin = "dark" Then
            patternpage &= "<BODY COLOR=#FFFFFF BGCOLOR=#000000>" & vbCrLf
        ElseIf skin = "cosmic" Then
            patternpage &= "<BODY COLOR=#000000 BGCOLOR=#EAEAEA BACKGROUND=""cosmic.gif"">" & vbCrLf
        Else
            patternpage &= "<BODY COLOR=#000000 BGCOLOR=#FFFFFF>" & vbCrLf
        End If

        Dim used_logo As String = "yt_logo2.gif"

        Select Case skin
            Case "oldyt"
                used_logo = "yt_logo.gif"
            Case "cosmic"
                used_logo = "yt_logo2.gif"
            Case "dark"
                used_logo = "yt_dark.gif"
            Case Else
                used_logo = "yt_modrn.gif"
        End Select

        'La tête de page pour rechercher des vidéos. Ce formulaire est présent sur chaque page naviguée.
        patternpage &= " <FORM METHOD=""GET"" ACTION=""/search"">" & vbCrLf
        patternpage &= " <CENTER><TABLE BORDER=0 WIDTH=900 ALIGN=CENTER>" & vbCrLf
        patternpage &= "  <TR>" & vbCrLf
        patternpage &= "   <TD WIDTH=90>&nbsp;</TD>"
        patternpage &= "   <TD WIDTH=120><A HREF=""/""><IMG SRC=""" & used_logo & """ BORDER=0 ALT=""Logo RetroYT"" HEIGHT=44 /></A></TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=400>&nbsp;&nbsp;<INPUT NAME=""q"" VALUE=""" & k & """ STYLE=""width: 380px;""></INPUT></TD>" & vbCrLf
        patternpage &= "   <TD WIDTH=*><INPUT TYPE=""SUBMIT"" VALUE=""Rechercher"" WIDTH=400 /> &nbsp; <A HREF=""/config"" STYLE=""color: darkred;"">Paramètres</A></TD>" & vbCrLf
        patternpage &= "  </TR>" & vbCrLf
        patternpage &= " </TABLE></CENTER>" & vbCrLf
        patternpage &= " </FORM><BR><BR><HR WIDTH=880 ALIGN=CENTER />" & vbCrLf & vbCrLf
    End Sub

    Function GetClientIP(client As TcpClient) As String
        'Obtenir l'adresse IP du client
        Return CType(client.Client.RemoteEndPoint, IPEndPoint).Address.ToString()
    End Function

    Function CleanText(input As String) As String
        Dim text As String = input

        text = text.Replace("+", " ")
        text = Uri.UnescapeDataString(text)

        ' remove non Latin-1
        Dim sb As New Text.StringBuilder()
        For Each c As Char In text
            If AscW(c) >= 32 AndAlso AscW(c) <= 255 Then
                sb.Append(c)
            End If
        Next
        text = sb.ToString()

        ' normalize spaces
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
        Dim months() As String = {"0", "jan.", "fév.", "mar.", "avr.", "mai", "juin", "juill.", "août", "sept.", "oct.", "nov.", "déc."}
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
        h = h.Replace("<", String.Empty)
        h = h.Replace(">", String.Empty)
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

    Sub Main(args As String())

        'L'application démarre ici!
        Console.Title = "RetroYT"

        Console.ForegroundColor = ConsoleColor.Green
        Console.WriteLine("******************************")
        Console.WriteLine("*      RetroYT Bêta 2.2      *")
        Console.WriteLine("******************************")
        Console.WriteLine()
        Console.ForegroundColor = ConsoleColor.Gray

        WriteLog("Initialisation du serveur mandataire en cours...")

        If Not IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) & "\yt-dlp.exe") Then
            WriteLog("yt-dlp.exe est absent dans le dossier système Windows. Exécution impossible.")
            WriteLog("Veuillez placer yt-dlp.exe dans sa dernière version dans " & Environment.GetFolderPath(Environment.SpecialFolder.System))
            Console.ReadKey()
            End
        End If

        'Code triche pour récupérer le chemin du lecteur local
        Dim localdrive As String = Environment.GetFolderPath(Environment.SpecialFolder.Windows).Substring(0, 3)

        If Not IO.File.Exists(localdrive & "ffmpeg\ffmpeg.exe") Then
            WriteLog("ffmpeg.exe est absent dans le dossier " & localdrive & "ffmpeg. Exécution impossible.")
            WriteLog("Veuillez installer la version essentials de FFMPEG dans ce dossier.")
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
            WriteLog("Pour changer le numéro, lancez YTSrv avec le numéro de port en paramètre immédiat.")
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

        Console.WriteLine()
        WriteLog("Serveur lancé sur le port " & port.ToString & " avec succès ! En attente de connexions...")

        If port = 80 Then
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez sur http://localhost/")
        Else
            WriteLog("Pour accéder au proxy, démarrez un navigateur ancien, et naviguez sur http://localhost:" & port.ToString & "/")
        End If

        WriteLog("Veuillez appuyer sur CTRL+C pour arrêter le serveur.")
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
        'Prise en charge des requêtes par le client
        Dim stream = client.GetStream()

        'Lire la requête HTTP
        Dim buffer(8192) As Byte
        Dim bytesRead = stream.Read(buffer, 0, buffer.Length)
        Dim request As String = iso.GetString(buffer, 0, bytesRead)
        Dim wanted_skin As String = "cosmic"

        'Afficher le cookie envoyé par le client
        If request.Contains("Cookie: retroyt=") Then
            Dim cookie1 As Integer = request.IndexOf("Cookie: ") + 16
            Dim cookie2 As Integer = request.IndexOf(vbCrLf, cookie1)
            Dim fullcookie As String = request.Substring(cookie1, cookie2 - cookie1)

            If cookie2 <> -1 Then

                WriteLog("Cookie envoyé par le client: " & fullcookie, ConsoleColor.Yellow, client)
                If fullcookie.Contains("skin=oldyt") Then wanted_skin = "oldyt"
                If fullcookie.Contains("skin=dark") Then wanted_skin = "dark"
                If fullcookie.Contains("skin=cosmic") Then wanted_skin = "cosmic"
                If fullcookie.Contains("skin=modern") Then wanted_skin = "modern"
            End If
        End If

        'Ecriture de la commande dans les rapports de connexion
        WriteLog("[" & Date.Now.ToShortDateString & " à " & Date.Now.ToShortTimeString & "] Requête entrante détectée en provenance de " & GetClientIP(client) & "...", ConsoleColor.White)

        If String.IsNullOrEmpty(request) Then
            'Requête vide
            WriteLog("Erreur 400: Requête vide envoyée.", , client)
            Dim compiled_bad As String = "<h1>Error 400: Internal Server Error</h1>" & vbCrLf &
           "<p>HTTP request was empty.</p>" & vbCrLf & vbCrLf

            Dim badresp As String =
           "HTTP/1.0 400 Bad Request" & vbCrLf &
           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
           "Content-Length: " & iso.GetBytes(compiled_bad).Length.ToString &
           "Connection: close" & vbCrLf &
           "Accept-Ranges: text" & vbCrLf & vbCrLf & compiled_bad

            Dim baddata As Byte() = iso.GetBytes(badresp)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception

            End Try

            client.Close()
        ElseIf request.StartsWith("GET /watch?v=") Then
            'Demande de lecture d'une vidéo par le client
            Dim watcharg As String = Split(request)(1)
            watcharg = watcharg.Remove(0, 9)

            Dim player_size As String = "middle" 'Paramètres par défaut
            Dim used_codec As String = "mpeg4"
            Dim used_player As String = "wmp"
            Dim ultra_legacy As Boolean = False

            'Obtenir le cookie du client
            If request.Contains("Cookie: ") Then
                If request.Contains("playersize=middle") Then player_size = "middle"
                If request.Contains("playersize=small") Then player_size = "small"
                If request.Contains("playersize=large") Then player_size = "large"
                If request.Contains("playersize=auto") Then player_size = "auto"

                If request.Contains("usedcodec=mpeg4") Then used_codec = "mpeg4"
                If request.Contains("usedcodec=msvideo1") Then used_codec = "msvideo1"
                If request.Contains("usedcodec=mp4") Then used_codec = "mp4"

                If request.Contains("usedplayer=legacy") Then used_player = "legacy" 'Le lecteur Windows Media intégré (Version 6.4)
                If request.Contains("usedplayer=wmp") Then used_player = "wmp" 'Le lecteur Windows Media intégré (Version 7.0 ou plus)
                If request.Contains("usedplayer=embed") Then used_player = "embed" 'Balise <embed> de HTML 4.0
                If request.Contains("usedplayer=video") Then used_player = "video" 'Balise <video> de HTML 5.0
            Else
                'Si aucun cookie n'est précisé, et que le flag legacy est activé (en cas de défaillance technique ou navigateur trop ancien)
                If watcharg.Contains("legacy=true") Then
                    player_size = "small"
                    used_codec = "msvideo1"
                    used_player = "legacy"
                    ultra_legacy = True
                End If
            End If

            'On retire les paramètres qui suivent "&".
            If watcharg.Contains("&") Then
                watcharg = watcharg.Substring(0, watcharg.IndexOf("&"))
            End If

            Dim output_path As String = Nothing 'Fichier généré
            Dim output_filename As String = Nothing 'Nom du fichier généré, sans le chemin

            'En fonction du codec/format vidéo demandé, on génère output_id_.avi (MPEG4), output_id_low.avi (MSVideo1), ou output_id.mp4 (MP4 H.264), où id correspond à l'identifiant de la vidéo YouTube voulue.
            Select Case used_codec
                Case "mpeg4"
                    output_path = CurDir() & "\vidcache\output_" & watcharg & ".avi"
                    output_filename = "output_" & watcharg & ".avi"
                Case "msvideo1"
                    output_path = CurDir() & "\vidcache\output_" & watcharg & "_low.avi"
                    output_filename = "output_" & watcharg & "_low.avi"
                Case "mp4"
                    output_path = CurDir() & "\vidcache\output_" & watcharg & ".mp4"
                    output_filename = "output_" & watcharg & ".mp4"
            End Select

            'Suppression des fichiers temporaires
            If IO.File.Exists(CurDir() & "\output.mp4.part") Then
                IO.File.Delete(CurDir() & "\output.mp4.part")
            End If

            If IO.File.Exists(CurDir() & "\output.part") Then
                IO.File.Delete(CurDir() & "\output.part")
            End If

            'Début du traitement de la requête. On vérifie si l'ID est valide (pas s'il existe).
            If LooksLikeYoutubeID(watcharg) Then
                last_view = watcharg

                WriteLog("Vidéo demandée: https://www.youtube.com/watch?v=" & last_view, ConsoleColor.Green, client)

                'Si la vidéo n'est pas en cache, le logiciel va interroger yt-dlp pour l'obtenir.
                If Not IO.File.Exists(output_path) Then

                    WriteLog("Téléchargement en cours...")
                    'Exécution du processus d'obtention de la vidéo souhaitée.
                    Dim psi As New ProcessStartInfo()
                    psi.FileName = "C:\Windows\system32\yt-dlp.exe"
                    psi.Arguments = "-f 18 --no-part --no-continue -o """ & finalpath & """ ""https://www.youtube.com/watch?v=" & last_view & """"
                    psi.UseShellExecute = False
                    psi.CreateNoWindow = True
                    psi.RedirectStandardOutput = True
                    psi.RedirectStandardError = True

                    'Call Process.Start(psi)
                    Dim p = Process.Start(psi)
                    Dim output = p.StandardOutput.ReadToEnd()
                    Dim err = p.StandardError.ReadToEnd()
                    p.WaitForExit()

                    'Affichage du résultat dans la fenêtre
                    WriteLog(output, ConsoleColor.Cyan)
                    WriteLog(err, ConsoleColor.Red)

                    WriteLog("Conversion du fichier MP4 trouvé vers le format voulu...")

                    If used_codec = "mp4" Then
                        'Transfert du MP4 tel quel
                        IO.File.Copy(finalpath, output_path)
                    Else
                        Dim psi2 As New ProcessStartInfo()
                        psi2.FileName = "C:\ffmpeg\ffmpeg.exe"

                        'Sinon, transfert au format MPEG4 (pour les Windows récents), ou MSVideo1 (pour Windows NT/9x)
                        If used_codec = "mpeg4" Then
                            psi2.Arguments = "-i """ & finalpath & """ -vf scale=-2:480 -r 25 -vcodec mpeg4 -b:v 800k -acodec mp3 -b:a 96k """ & output_path & """"
                        Else
                            psi2.Arguments = "-i """ & finalpath & """ -vf scale=320:240 -r 25 -c:v msvideo1 -c:a pcm_s16le """ & output_path & """"
                        End If

                        psi2.UseShellExecute = False
                        psi2.CreateNoWindow = True

                        Dim p2 = Process.Start(psi2)
                        p2.WaitForExit()
                    End If
                Else
                    WriteLog("Vidéo déjà en cache. Envoi direct du fichier.")
                End If

                'Mise en cache du titre (et de l'ID)
                Dim tmp_title As String = "(Titre inconnu)"
                If last_titles.ContainsKey(watcharg) Then
                    tmp_title = last_titles(watcharg)
                Else
                    'Choper le titre en ligne, s'il venait à manquer.
                    Dim psi3 As New ProcessStartInfo()
                    psi3.FileName = "C:\Windows\system32\yt-dlp.exe"
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

                    p3.WaitForExit()
                End If

                InitValues("Visualisation", , wanted_skin)
                patternpage &= "<CENTER><DIV WIDTH=900 ALIGN=CENTER><BR>" & vbCrLf
                patternpage &= "<P ALIGN=CENTER><B><FONT SIZE=4>" & EscapeHtml(tmp_title) & "</FONT></B></P><BR>" & vbCrLf
                'patternpage &= "<EMBED SRC=""/output.avi"" TYPE=""video/x-msvideo"" WIDTH=380 HEIGHT=240 ALIGN=CENTER AUTOSTART=TRUE SHOWCONTROLS=TRUE /><BR><BR><BR>" & vbCrLf

                Dim player_width, player_height As Integer

                'Détermination de la taille du lecteur via le cookie
                Select Case player_size
                    Case "small"
                        'Petit lecteur, utile pour les petits écrans
                        player_width = 320
                        player_height = 240
                    Case "middle"
                        'Moyen lecteur (VGA)
                        player_width = 640
                        player_height = 480
                    Case "large"
                        'Lecteur large, pouvant aisément afficher du 16:9
                        player_width = 854
                        player_height = 480
                    Case "auto"
                        player_width = 640
                        player_height = 480 'Failsafe

                        'Utilisation du Javascript pour redimensionner de façon dynamique le lecteur intégré.
                        patternpage &= "<script>" & vbCrLf
                        patternpage &= " function resizePlayer() {" & vbCrLf
                        patternpage &= "  var player = document.getElementById(""mainplayer"");" & vbCrLf & vbCrLf

                        patternpage &= "  var winW = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;" & vbCrLf
                        patternpage &= "  var winH = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;" & vbCrLf & vbCrLf

                        patternpage &= "  // marges" & vbCrLf
                        patternpage &= "  var maxW = winW - 40;" & vbCrLf
                        patternpage &= "  var maxH = winH - 120;" & vbCrLf & vbCrLf

                        patternpage &= "  // ratio 4:3" & vbCrLf
                        patternpage &= "  var ratioW = 4;" & vbCrLf
                        patternpage &= "  var ratioH = 3;" & vbCrLf & vbCrLf

                        patternpage &= "  // calcul basé sur largeur" & vbCrLf
                        patternpage &= "  var width = maxW;" & vbCrLf
                        patternpage &= "  var height = Math.floor(width * ratioH / ratioW);" & vbCrLf & vbCrLf

                        patternpage &= "  // si ça dépasse en hauteur -> recalcul" & vbCrLf
                        patternpage &= "  if (height > maxH) {" & vbCrLf
                        patternpage &= "   height = maxH;" & vbCrLf
                        patternpage &= "   width = Math.floor(height * ratioW / ratioH);" & vbCrLf
                        patternpage &= "  }" & vbCrLf & vbCrLf

                        patternpage &= "  // minimum" & vbCrLf
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

                'Le lecteur intégré
                Select Case used_player
                    Case "legacy"
                        'Ancien lecteur Windows Media (6.4) intégré avec la balise <object>.
                        patternpage &= "<object id=""mainplayer"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ classid=""CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95"">" & vbCrLf 'Usage du contrôle Windows Media Player 6.4
                        patternpage &= " <param name=""FileName"" value=""/" & output_filename & """>" & vbCrLf
                        patternpage &= " <param name=""AutoStart"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""EnableFullScreenControls"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""VideoBorder3D"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""StretchToFit"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""ShowControls"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""DisplaySize"" value=4>" & vbCrLf
                        patternpage &= " <param name=""DefaultFrame"" value=""/thumbnail?t=" & last_view & """>" & vbCrLf
                        patternpage &= "</object>" & vbCrLf
                    Case "wmp"
                        'Nouveau lecteur Windows Media (7.0 et +) intégré avec la balise <object>.
                        patternpage &= "<object id=""mainplayer"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ classid=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"">" & vbCrLf 'Usage du contrôle Windows Media Player 7.0 et plus
                        patternpage &= " <param name=""URL"" value=""/" & output_filename & """>" & vbCrLf
                        patternpage &= " <param name=""AutoStart"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""EnableFullScreenControls"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""VideoBorder3D"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""StretchToFit"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""ShowControls"" value=""true"">" & vbCrLf
                        patternpage &= " <param name=""DefaultFrame"" value=""/thumbnail?t=" & last_view & """>" & vbCrLf
                        patternpage &= "</object>" & vbCrLf
                    Case "embed"
                        'Balise <embed>, une syntaxe et un fonctionnement lancés par NetScape en 1995.
                        If used_codec = "mp4" Then
                            patternpage &= "<embed id=""mainplayer"" src=""/" & output_filename & """ type=""video/mp4"" width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autostart=""true""></embed>"
                        Else
                            patternpage &= "<embed id=""mainplayer"" src=""/" & output_filename & """ type=""video/x-msvideo""  width=""" & player_width.ToString & """ height=""" & player_height.ToString & """ autostart=""true""></embed>"
                        End If
                    Case "video"
                        'Balise <video> de HTML 5.0 (W3C)
                        patternpage &= "<video id=""mainplayer"" controls width=""" & player_width.ToString & """ height=""" & player_height.ToString & """>" & vbCrLf 'style=""object-fit: fill;""

                        If used_codec = "mp4" Then
                            patternpage &= " <source src=""/" & output_filename & """ type=""video/mp4"" />" & vbCrLf
                        Else
                            patternpage &= " <source src=""/" & output_filename & """ type=""video/x-msvideo"" />" & vbCrLf
                        End If

                        patternpage &= "</video>" & vbCrLf
                End Select

                'Dans certains cas, le lecteur ne peut pas être disponible, alors on propose tout de même un lien en flux direct, ou pour "forcer" le mode rétrocompatible. Compatibilité garantie sur les très anciens navigateurs.

                If ultra_legacy Then
                    patternpage &= "<P ALIGN=CENTER>Cliquez <A HREF=""/" & output_filename & """ STYLE=""color: darkred;"">ici</A> pour accéder au flux direct, si la vidéo ne démarre toujours pas.</P>" & vbCrLf
                Else
                    patternpage &= "<P ALIGN=CENTER>Cliquez <A HREF=""/" & output_filename & """ STYLE=""color: darkred;"">ici</A> pour accéder au flux direct, ou <A HREF=""/watch?v=" & last_view & "&legacy=true"" STYLE=""color: darkred;"">ici</a> pour forcer le mode rétrocompatibilité.</P>" & vbCrLf
                End If

                patternpage &= "</DIV></CENTER><BR><DIV CLASS=""bodysep""></DIV><BR>" & footer & vbCrLf & vbCrLf

                Dim watch_resp As String =
                    "HTTP/1.0 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

                Dim watch_bytes As Byte() = iso.GetBytes(watch_resp)

                stream.Write(watch_bytes, 0, watch_bytes.Length)
            Else
                'Identifiant invalide manifestement!
                InitValues("Erreur de saisie", , wanted_skin)
                patternpage &= " <P ALIGN=CENTER><BR><B>L'identifiant vidéo que vous avez entré semble invalide. Aucune lecture ne peut être poursuivie.<br><br>Cliquez <A HREF=""/"" STYLE=""color: darkred;"">ici</A> pour retourner à l'index.</B></P><BR><BR></BODY></HTML>" & vbCrLf & vbCrLf

                Dim watch_resp As String =
                    "HTTP/1.0 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

                Dim baddata As Byte() = iso.GetBytes(watch_resp)

                Try
                    stream.Write(baddata, 0, baddata.Length)
                Catch ex As Exception

                End Try
            End If

            client.Close()
        ElseIf request.StartsWith("GET /search?q=") Then
            'Lancement d'une recherche par l'utilisateur.
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
            'req = req.Replace("'", " ")

            If Not String.IsNullOrEmpty(req) Then
                WriteLog("Recherche du mot-clef '" & req & "' demandée...", ConsoleColor.White, client)

                Dim number_of_results As Integer = 10

                If request.Contains("Cookie: ") AndAlso request.Contains("results=") AndAlso request.Contains("&") Then
                    'Application du paramètre du cookie
                    Dim c1, c2 As Integer
                    c1 = request.IndexOf("results=")
                    c2 = request.IndexOf("&", c1)

                    If c2 <> -1 Then
                        Dim s As String = request.Substring(c1, c2 - c1)
                        s = s.Remove(0, 8)
                        If IsNumeric(s) Then
                            Try
                                Dim temp_result As Integer = CInt(s)
                                If temp_result >= 5 And temp_result <= 20 Then
                                    number_of_results = temp_result
                                End If
                            Catch ex As Exception
                                number_of_results = 10
                            End Try
                        End If
                    End If
                End If

                'Lancement de yt-dlp
                Dim psi As New ProcessStartInfo()
                psi.FileName = "C:\Windows\system32\yt-dlp.exe"
                psi.Arguments = "--print ""%(id)s<|>%(title)s<|>%(view_count)s<|>%(upload_date)s<|>%(uploader)s<|>%(thumbnail)s<|>%(duration)s"" ""ytsearch" & number_of_results.ToString & ":" & req & """ --no-warnings --encoding utf-8"

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
                Else
                    'Sinon, on affiche les résultats dans la page Web.
                    If lines.Count = 1 Then
                        patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Le meilleur résultat pour la recherche de « " & EscapeHtml(req) & " » :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                    Else
                        patternpage &= " <P ALIGN=CENTER><BR><BR><B><FONT SIZE=4>Les " & lines.Count.ToString & " meilleurs résultats pour la recherche de « " & EscapeHtml(req) & " » :</FONT></B></P><BR><BR>" & vbCrLf & vbCrLf
                    End If
                    patternpage &= "  <CENTER><TABLE BORDER=0 CELLPADDING=8 WIDTH=900 ALIGN=CENTER>" & vbCrLf

                    For Each line In lines

                        Dim parts = line.Split(New String() {"<|>"}, StringSplitOptions.None)

                        If parts.Length >= 6 Then
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

                            'thumb =
                            Dim legacy_flag As String = String.Empty
                            'If request.Contains("MSIE 5") Or request.Contains("MSIE 4") Then legacy_flag = "&legacy=true"

                            'Affichage d'une ligne dans les recherches, sous la forme d'une miniature accompagnée de quelques métadonnées.
                            patternpage &= "   <TR>" & vbCrLf
                            patternpage &= "    <TD WIDTH=200><A HREF=""/watch?v=" & id & legacy_flag & """><IMG SRC=""" & thumb & """ WIDTH=180 HEIGHT=120 CLASS=""thumbstyle"" BORDER=0 ALT=""" & EscapeHtml(title) & """ /></A></TD>" & vbCrLf
                            patternpage &= "    <TD WIDTH=* VALIGN=TOP><BR><A HREF=""/watch?v=" & id & legacy_flag & """>" & EscapeHtml(title) & " <FONT COLOR=""#808080"">(" & GetDuration(CInt(duration)) & ")</FONT><BR>" & GetThousands(views) & " vue(s)</A><BR><BR>Vidéo publiée le " & GetDate(dateup) & " par <I>" & EscapeHtml(uploader) & "</I>.</TD>" & vbCrLf
                            patternpage &= "   </TR>" & vbCrLf

                        End If

                    Next

                    patternpage &= "  </TABLE></CENTER>"
                End If

                patternpage &= "<BR><BR>" & footer

                'Envoi du résultat à l'utilisateur via une réponse HTTP favorable.
                Dim req_resp As String =
                    "HTTP/1.0 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

                'Conversion en octets, suivant le format ISO-8859-1.
                Dim req_data As Byte() = iso.GetBytes(req_resp)

                Try
                    'Ecriture dans le flux octal en direction du client.
                    stream.Write(req_data, 0, req_data.Length)
                Catch ex As Exception

                End Try
            Else
                'Si le mot-clef est vide voire invalide.
                InitValues("Erreur de recherche", , wanted_skin)
                patternpage &= " <P ALIGN=CENTER><BR><B><FONT SIZE=2>Veuillez spécifier un mot-clef pour que la recherche puisse avoir lieu.<BR><BR>Cliquez <A HREF=""/"" STYLE=""color: darkred;"">ici</A> pour retourner à l'index.</FONT></B></P><BR><BR><DIV CLASS=""bodysep""></DIV>" & vbCrLf & vbCrLf & footer

                Dim req_resp As String =
                    "HTTP/1.0 200 OK" & vbCrLf &
                    "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                    "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                    "Connection: close" & vbCrLf &
                    "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

                Dim req_data As Byte() = iso.GetBytes(req_resp)

                Try
                    stream.Write(req_data, 0, req_data.Length)
                Catch ex As Exception

                End Try
            End If

            client.Close()

        ElseIf request.StartsWith("GET /stream") Then
            'Peut-être à retirer... ça aurait été une alternative directe à watch. watch se contentait d'intégrer le lien vers la vidéo sans la télécharger, mais maintenant,
            'il le fait aussi. De plus, on peut appeler directement le fichier "/output_id.avi", par exemple, au cas où il serait déjà téléchargé. /stream devait être sans paramètre,
            'et permettre de lire le dernier flux récupéré en mémoire.

            Dim compiled_bad As String = "<h1>Error 400: Internal Server Error</h1>" & vbCrLf &
           "<p>This feature is not implemented yet...</p>" & vbCrLf & vbCrLf

            Dim badresp As String =
           "HTTP/1.0 400 Bad Request" & vbCrLf &
           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
           "Content-Length: " & compiled_bad.Length.ToString &
           "Connection: close" & vbCrLf &
           "Accept-Ranges: text" & vbCrLf & vbCrLf & compiled_bad

            Dim baddata As Byte() = iso.GetBytes(badresp)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception

            End Try

            client.Close()
        ElseIf request.StartsWith("GET /thumbnail?t=") Then
            'Miniatures YouTube
            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 13)
            WriteLog("Miniature de la vidéo pour id '" & arg & "' demandée... ", , client)
            Dim path As String = "thumbs\" & arg & ".jpg"

            'https://i.ytimg.com/vi/xxxxxxxxxxx/default.jpg

            If Not IO.File.Exists(path) Then
                Dim url As String = "https://i.ytimg.com/vi/" & arg & "/mqdefault.jpg"

                Try
                    Dim wc As New Net.WebClient()
                    wc.DownloadFile(url, path)
                    WriteLog("Miniature avec pour id '" & arg & "'  envoyée !")
                Catch ex As Exception
                    path = CurDir() & "\resfiles\nopic.jpg"
                    WriteLog("Pas de miniature trouvée ! Envoi d'une miniature par défaut...")
                End Try
            End If

            Dim bytes = IO.File.ReadAllBytes(path)

            Dim header As String =
                "HTTP/1.0 200 OK" & vbCrLf &
                "Content-Type: image/jpeg" & vbCrLf &
                "Connection: close" & vbCrLf &
                "Content-Length: " & bytes.Length & vbCrLf & vbCrLf

            Try
                stream.Write(iso.GetBytes(header), 0, iso.GetBytes(header).Length)
                stream.Write(bytes, 0, bytes.Length)
            Catch ex As Exception

            End Try

            client.Close()
        ElseIf request.StartsWith("GET /output_") Then
            Dim arg1 As String = request.Remove(0, 5)
            arg1 = arg1.Substring(0, arg1.IndexOf(" "))

            If Not IO.File.Exists(CurDir() & "\vidcache\" & arg1) Then
                Dim compiled_text As String = "<h1>500 Error - Internal server error</h1>" & vbCrLf &
                "<p>Video with id '<i>" & last_view & "</i>' was not found on YouTube servers.</p>" & vbCrLf
                Dim notfound_resp As String =
                "HTTP/1.0 500 Internal Error" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(compiled_text).Length.ToString & vbCrLf &
                "Cache-Control: no-cache, no-store, must-revalidate" & vbCrLf &
                "Pragma: no-cache" & vbCrLf &
                "Expires: 0" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & compiled_text

                Dim notfound_data As Byte() = iso.GetBytes(notfound_resp)

                Try
                    stream.Write(notfound_data, 0, notfound_data.Length)
                Catch ex As Exception

                End Try

                client.Close()
                WriteLog("Video with id '" & last_view & "' was not found, or file has been removed.")
            Else
                Try
                    Dim sent_res As String = "HTTP/1.0 200 OK" & vbCrLf
                    Dim sent_data As Byte()
                    sent_res &= "Content-Type: video/x-msvideo" & vbCrLf
                    sent_res &= "Connection: close" & vbCrLf
                    sent_res &= "Content-Length: " & FileLen(CurDir() & "\vidcache\" & arg1).ToString & vbCrLf & vbCrLf
                    sent_data = iso.GetBytes(sent_res)

                    Try
                        stream.Write(sent_data, 0, sent_data.Length)
                    Catch ex As Exception

                    End Try


                    Dim fs As System.IO.FileStream = Nothing
                    Dim resBuffer(8191) As Byte ' 8 Ko
                    Dim resread As Integer = 0

                    fs = New System.IO.FileStream(CurDir() & "\vidcache\" & arg1, IO.FileMode.Open, IO.FileAccess.Read)

                    Do
                        resread = fs.Read(resBuffer, 0, resBuffer.Length)
                        If resread = 0 Then Exit Do

                        Try
                            stream.Write(resBuffer, 0, resread)
                        Catch ex As Exception
                            Exit Do
                        End Try
                    Loop

                    fs.Close()
                Catch ex As Exception

                End Try

                client.Close()
            End If
        ElseIf request.StartsWith("GET /config") Then
            'Montrer le panneau de configuration client du navigateur

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

            Dim selected_five As String = String.Empty
            Dim selected_ten As String = String.Empty
            Dim selected_twenty As String = String.Empty

            Dim selected_small As String = String.Empty
            Dim selected_middle As String = String.Empty
            Dim selected_large As String = String.Empty
            Dim selected_auto As String = String.Empty

            Dim selected_mpeg4 As String = String.Empty
            Dim selected_msvideo As String = String.Empty
            Dim selected_mp4 As String = String.Empty

            Dim selected_legacy As String = String.Empty
            Dim selected_wmp As String = String.Empty
            Dim selected_embed As String = String.Empty
            Dim selected_video As String = String.Empty

            Dim selected_classic As String = String.Empty
            Dim selected_cosmic As String = String.Empty
            Dim selected_modern As String = String.Empty
            Dim selected_dark As String = String.Empty

            If request.Contains("Cookie: ") Then
                If request.Contains("results=5") Then selected_five = " SELECTED"
                If request.Contains("results=10") Then selected_ten = " SELECTED"
                If request.Contains("results=20") Then selected_twenty = " SELECTED"

                If request.Contains("playersize=small") Then selected_small = " SELECTED"
                If request.Contains("playersize=middle") Then selected_middle = " SELECTED"
                If request.Contains("playersize=large") Then selected_large = " SELECTED"
                If request.Contains("playersize=auto") Then selected_auto = " SELECTED"

                If request.Contains("usedcodec=mp4") Then selected_mp4 = " SELECTED"
                If request.Contains("usedcodec=msvideo1") Then selected_msvideo = " SELECTED"
                If request.Contains("usedcodec=mpeg4") Then selected_mpeg4 = " SELECTED"

                If request.Contains("usedplayer=legacy") Then selected_legacy = " SELECTED"
                If request.Contains("usedplayer=wmp") Then selected_wmp = " SELECTED"
                If request.Contains("usedplayer=embed") Then selected_embed = " SELECTED"
                If request.Contains("usedplayer=video") Then selected_video = " SELECTED"

                If request.Contains("skin=oldyt") Then selected_classic = " SELECTED"
                If request.Contains("skin=cosmic") Then selected_cosmic = " SELECTED"
                If request.Contains("skin=modern") Then selected_modern = " SELECTED"
                If request.Contains("skin=dark") Then selected_dark = " SELECTED"
            Else
                selected_ten = " SELECTED"
                selected_middle = " SELECTED"
                selected_embed = " SELECTED"
                selected_cosmic = " SELECTED"
            End If

            InitValues("Configuration client", , wanted_skin)
            patternpage &= "<BR><P ALIGN=CENTER><B><FONT SIZE=4>Configuration du client RetroYT :</FONT></B></P><br>" & vbCrLf & vbCrLf

            patternpage &= "  <FORM METHOD=""POST"" ACTION=""/saveconfig"">" & vbCrLf
            patternpage &= "   <CENTER><TABLE BORDER=0 ALIGN=CENTER WIDTH=900>" & vbCrLf
            patternpage &= "    <TR>" & vbCrLf
            patternpage &= "	 <TD>Nombre de résultats affichés par recherche :</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""results"">" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""5""" & selected_five & ">5</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""10""" & selected_ten & ">10</OPTION>" & vbCrLf
            patternpage &= "       <OPTION VALUE=""20""" & selected_twenty & ">20</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD>Taille du lecteur multimédia intégré :</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""playersize"">" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""small""" & selected_small & ">Compact (320x240)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""middle""" & selected_middle & ">Standard (640x480)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""large""" & selected_large & ">Large (854x480)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""auto""" & selected_auto & ">Automatique</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD>Format vidéo / codec utilisé :</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""usedcodec"">" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mpeg4""" & selected_mpeg4 & ">Format AVI (codec MPEG4) en 480p</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""msvideo1""" & selected_msvideo & ">Format AVI (codec MSVideo1) en 320x240</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""mp4""" & selected_mp4 & ">Format MP4 original</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf
            patternpage &= "	 <TD>Intégration multimédia utilisée :</TD>" & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""usedplayer"">" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""legacy""" & selected_legacy & ">Objet Windows Media Player 6.4</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""wmp""" & selected_wmp & ">Objet Windows Media Player 7.0 et plus</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""embed""" & selected_embed & ">Balise embed (Par défaut)</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""video""" & selected_video & ">Balise video (HTML 5.0)</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf & vbCrLf

            patternpage &= "	<TR>" & vbCrLf & vbCrLf
            patternpage &= "	 <TD>Apparence du site :</TD>" & vbCrLf & vbCrLf
            patternpage &= "	 <TD HEIGHT=40>" & vbCrLf
            patternpage &= "	  <SELECT NAME=""skin"">" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""oldyt""" & selected_classic & ">Classic</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""cosmic""" & selected_cosmic & ">Cosmic Tube</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""modern""" & selected_modern & ">Modern</OPTION>" & vbCrLf
            patternpage &= "	   <OPTION VALUE=""dark""" & selected_dark & ">Dark Mode</OPTION>" & vbCrLf
            patternpage &= "	  </SELECT>" & vbCrLf
            patternpage &= "	 </TD>" & vbCrLf
            patternpage &= "	</TR>" & vbCrLf
            patternpage &= "   </TABLE></CENTER><BR><BR>" & vbCrLf & vbCrLf

            patternpage &= "   <CENTER><P>Cliquez sur le bouton pour <INPUT TYPE=""SUBMIT"" VALUE=""Enregistrer"" /> ou sur le lien <A HREF=""/resetconfig"" STYLE=""color: darkred;"">réinitialiser les paramètres</A>.</P></CENTER>" & vbCrLf
            patternpage &= "  </FORM><BR>" & vbCrLf
            patternpage &= "  <P ALIGN=CENTER><B>Nota: Le conteneur AVI produisant des vidéos assez lourdes, veillez à<BR>ne pas lire des vidéos trop longues sur des anciennes configurations.<BR><BR>Il est conseillé d'utiliser le codec MSVideo1 si vous visualisez vos vidéos depuis les<BR>systèmes suivants: Windows 3.11, 95, NT 3.x, NT 4.0, 98(SE), et Millenium Edition.</B><BR><BR><NOSCRIPT><B>Par ailleurs, La taille automatique du lecteur n'est disponible que si Javascript est activé.<BR>Votre navigateur actuel semble ne pas le prendre en charge, ou Javascript a été désactivé.</B></NOSCRIPT></P><BR><BR><BR>" & vbCrLf
            patternpage &= " <BR><BR>" & footer

            Dim index_resp As String =
                "HTTP/1.0 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception

            End Try

            client.Close()

        ElseIf request.StartsWith("POST /saveconfig") Then
            'Sauvegarde de la configuration client
            Dim rqcontent As String = request.Remove(0, request.IndexOf(vbCrLf & vbCrLf) + 4)
            rqcontent = rqcontent.Trim()
            rqcontent = rqcontent.Replace(Chr(10), String.Empty)
            rqcontent = rqcontent.Replace(Chr(13), String.Empty)

            If String.IsNullOrEmpty(rqcontent) Then
                rqcontent = "results=10&playersize=middle&usedcodec=mpeg4&usedplayer=embed&skin=cosmic"
            End If

            Dim result_page As String = "<h1>302 Found</h1><p>Configuration has been saved, you can now navigate to <a href=""/config"">this page</a>.</p>" & vbCrLf & vbCrLf

            Dim index_resp As String =
                "HTTP/1.0 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Set-Cookie: retroyt=" & rqcontent & ";path=/" & vbCrLf &
                "Location: /config" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception

            End Try

            client.Close()

        ElseIf request.StartsWith("GET /resetconfig") Then
            'Réinitialiser la configuration client

            Dim result_page As String = "<h1>302 Found</h1><p>Configuration has been reset, you can now navigate to <a href=""/config"">this page</a>.</p>" & vbCrLf & vbCrLf

            Dim index_resp As String =
                "HTTP/1.0 302 Found" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(result_page).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Set-Cookie: retroyt=results=10&playersize=middle&usedcodec=mpeg4&usedplayer=embed&skin=cosmic;path=/" & vbCrLf &
                "Location: /config" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & result_page 'Petit message si le navigateur de l'utilisateur n'arrive pas à localiser

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception

            End Try

            client.Close()

        ElseIf request.StartsWith("GET /about") Then
            'Afficher le "à propos" du proxy
            InitValues("À propos de RetroYT", , wanted_skin)

            patternpage &= "<br><br><center><div style=""display: block; width: 900px; margin-left: auto; margin-right: auto; text-align: left; text-align: justify;""><B>RetroYT</B> est un proxy pour YouTube codé en Visual Basic .NET 2022 par Monokeros.<br>" & vbCrLf
            patternpage &= "La dernière version en Date, la Bêta 2.2, a été éditée le 17 avril 2026. Licence freeware sans garantie.<br>Il est important de préciser qu'aucun dommage matériel ou logiciel ne sera sous la responsabilité de l'auteur.<br><br><br>" & vbCrLf

            patternpage &= "<div style=""border: 1px solid black; padding: 8px 8px 8px 8px; width: 40%;""><b>Sommaire: </b><br>" & vbCrLf
            patternpage &= "<a href=""#introduction"" style=""color: darkred;"">I. Introduction</a><br>" & vbCrLf
            patternpage &= "<a href=""#parameters"" style=""color: darkred;"">II. Paramètres</a><br>" & vbCrLf
            patternpage &= "<a href=""#precautions"" style=""color: darkred;"">III. Précautions</a><br>" & vbCrLf
            patternpage &= "<a href=""#configuration"" style=""color: darkred;"">IV. Configuration</a><br>" & vbCrLf
            patternpage &= "<a href=""#credits"" style=""color: darkred;"">V. Crédits</a></div><br><br>" & vbCrLf & vbCrLf

            patternpage &= "<a name=""introduction"">I. Introduction</a><br><br>" & vbCrLf
            patternpage &= "Le nom provient de ""rétro"" signifiant, pour résumer de façon claire et globale, ""à l'ancienne"". Ce logiciel fait usage d'un serveur Web hardcodé, qui redirige les connexions entre le serveur YouTube et le client. Le but est de fournir une expérience de navigation sur YouTube aux navigateurs/systèmes trop anciens pour naviguer sur la version actuelle du site. Bien que ce proxy puisse être utilisé par un navigateur moderne, et ainsi servir de proxy ordinaire, ce n'est pas l'objectif principal. De nombreux proxies YouTube existent probablement déjà, fournissant de meilleurs résultats que mon projet. En pratique, le but de RetroYT est d'apporter une possibilité de rechercher et lire des vidéos YouTube depuis d'anciens systèmes comme Windows 98, Windows NT 4.0, Windows 2000, etc. Ne vous étonnez donc pas de voir du code HTML désuet, en regardant le code source des pages générées.<br><br>" & vbCrLf

            patternpage &= "<br><a name=""parameters"">II. Paramètres</a><br><br>" & vbCrLf
            patternpage &= "Dans les paramètres, l'utilisateur peut choisir un ensemble d'options pouvant adapter le proxy à sa configuration. La taille du lecteur peut être modifiée, ainsi que le codec utilisé pour la lecture des vidéos. Pour les anciens systèmes comme Windows 98, l'usage du codec MSVideo1 (Microsoft Video 1) est recommandé, même s'il produit des vidéos très lourdes, au-delà de 5 ou 10 minutes de longueur de vidéo. Le type d'intégration de lecteur vidéo peut être également paramétré. Vous pouvez par exemple activer la lecture vidéo via un objet Windows Media Player 6.4 ou plus récent, via la balise embed, ou via la balise video sur les navigateurs plus récents qui prennent en charge le HTML5.<br><br>" & vbCrLf
            patternpage &= "Vous pouvez aussi changer l'apparence de l'interface Web, en choisissant un thème parmi la liste suivante: Classic, Cosmic, Modern, et Dark. Le mode classique affiche une interface similaire au YouTube des années 2000. Cosmic est une réplique de l'interface <i>Cosmic Panda</i> en usage sur le site officiel entre 2011 et 2013. Le thème Moderne imite l'interface du YouTube actuel. Le mode Sombre active un affichage blanc sur fond noir, comme son nom le suggère.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><a name=""precautions"">III. Précautions</a><br><br>" & vbCrLf
            patternpage &= "Le proxy est sous licence freeware/open source, et ne doit pas être revendu. Il ne fait pas usage des technologies de chiffrement telles que SSL/TLS, pour rester compatible avec les anciens navigateurs. Il est programmé pour plutôt servir au sein d'un LAN, puisque toutes les communications entre le proxy et le client ne sont pas chiffrées du tout (seuls les échanges entre le serveur YouTube et le proxy le sont). Il n'est donc pas conseillé de l'utiliser sur un réseau public ou à travers Internet, sauf au cas où vous utiliseriez un VPN.<br>" & vbCrLf
            patternpage &= "Le proxy fait également usage d'un cache pour les miniatures (au format MQ, c'est-à-dire de qualité moyenne), et pour les vidéos visualisées. Les dossiers \thumbs\ et \vidcache\ peuvent être vidés, si vous estimez que l'espace disque vînt à manquer. En revanche, ni le dossier \resfiles\ ne doit pas être supprimé, ni les fichiers YTSrv.deps.json, YTSrv.runtimeconfig.json, YTSrv.dll, et YTSrv.pdb, car indispensables au bon fonctionnement du logiciel. Le fichier exécutable RetroYT.exe reste également indispensable au lancement du proxy, cela va sans dire.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><a name=""configuration"">IV. Configuration</a><br><br>" & vbCrLf
            patternpage &= "Côté serveur, sur lequel se lance le proxy, il est conseillé de le faire sur un ordinateur véloce, qui soit à jour et qui dispose d'une connexion Internet rapide. Windows 10 et 11 sont recommandés pour exécuter le serveur qui exige le .NET Framework 6.0 pour fonctionner. Côté client, j'ai pu tester avec succès le proxy sous les configurations suivantes :<br><br>" & vbCrLf & vbCrLf
            patternpage &= "<ul>" & vbCrLf
            patternpage &= " <li>Windows NT 4.0 SP6 / Internet Explorer 6.0 / Windows Media Player 6.4 / 1Go de RAM, 32Mo de mémoire vidéo et processeur de 700MHz ;</li>" & vbCrLf
            patternpage &= " <li>Windows 2000 SP4 / Internet Explorer 6.0 / Windows Media Player 9.0 / 3Go de RAM, 256Mo de mémoire vidéo et processeur de 1,85GHz ;</li>" & vbCrLf
            patternpage &= " <li>Windows ME / Internet Explorer 5.5 / Windows Media Player 7.0 / 1Go de RAM.</li>" & vbCrLf
            patternpage &= "</ul><br>" & vbCrLf & vbCrLf

            patternpage &= "Veillez à autoriser les contrôles ActiveX, à avoir un ou plusieurs lecteurs multimédias installés, et les cookies activés. Sinon, vous pourrez toujours forcer le mode rétrocompatibilité sur la page de visualisation, en cliquant sur le lien voulu. Pour les anciennes versions de Windows, activer le codec MSVideo1 depuis la section ""Paramètres"" est recommandé, même si les vidéos seront un peu lourdes. Il s'agit d'un codec avec compression intégrée, 100% compatible avec Windows, depuis la version 3.1. Pour les anciens navigateurs compatibles HTML5, vous pouvez activer le format MP4, et la balise video.<br>" & vbCrLf
            patternpage &= "Si par malheur aucune de ces options ne fonctionnent, vous pouvez également cliquer sur le lien pour lire le flux vidéo directement. Le navigateur ouvrira un lecteur externe, ou vous proposera de télécharger le fichier pour le lire après. Mais il s'agit d'une option de dernier recours. Notez bien que les URL sont prises en charge à partir de Windows Media Player 6.4.<br><br>" & vbCrLf & vbCrLf

            patternpage &= "<br><a name=""credits"">V. Crédits</a><br><br>" & vbCrLf
            patternpage &= "YouTube est une propriété de Google. Il s'agit d'une plateforme de diffusion de vidéos en direct, ou en différé. Ce projet de proxy n'est pas affilié à Google, ni à YouTube." & vbCrLf
            patternpage &= "Ce logiciel a été développé sous Microsoft Visual Basic .NET 2022. Il fait usage des librairies et binaires ffmpeg, et du projet yt-dlp. Merci à ChatGPT pour ses astuces de programmation. Sans lui, ce projet n'aurait peut-être jamais vu le jour. Merci également à vous, l'utilisateur, d'avoir utilisé ce logiciel, en espérant qu'il fonctionnera parfaitement sur votre configuration, et qu'il vous procurera entière satisfaction.<br><br><i>L'auteur.</i><br><br>" & vbCrLf & vbCrLf
            patternpage &= "<A HREF=""/"" STYLE=""color: darkred;"">Cliquez ici pour retourner à l'index</A><BR><BR>" & vbCrLf
            patternpage &= "</div></center>" & footer

            Dim index_resp As String =
                "HTTP/1.0 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

            Dim index_data As Byte() = iso.GetBytes(index_resp)

            Try
                stream.Write(index_data, 0, index_data.Length)
            Catch ex As Exception

            End Try

            client.Close()

        ElseIf request.StartsWith("GET /") Then
            'Autres requêtes GET
            request = request.Replace("../", String.Empty)
            request = request.Replace("./", String.Empty)
            request = request.Replace("/.", String.Empty)
            request = request.Replace("/..", String.Empty) 'Retirer les tentatives de consulter ce qui se situe dans le dossier parent

            For i As Integer = 0 To &H1F
                request = request.Replace(Chr(i), String.Empty)
            Next

            Dim arg As String = Split(request)(1)
            arg = arg.Remove(0, 1)

            Dim fs As System.IO.FileStream = Nothing
            Dim resBuffer(8191) As Byte ' 8 Ko
            Dim resread As Integer = 0

            If arg.Length = 0 Then
                'Index du site
                WriteLog("L'utilisateur demande l'index du site. Renvoi vers la page d'accueil.", , client)
                InitValues("Accueil", , wanted_skin)
                patternpage &= "<P ALIGN=CENTER><BR><B>Pour commencer, veuillez entrer un mot-clef à rechercher dans la zone ci-dessus.<BR><BR>Cliquez <A HREF=""/about"" STYLE=""color: darkred;"">ICI</A> pour obtenir plus d'informations.</B></P><DIV CLASS=""bodysep"" STYLE=""height: 500px;""></DIV><BR><BR>" & footer

                Dim index_resp As String =
                "HTTP/1.0 200 OK" & vbCrLf &
                "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                "Content-Length: " & iso.GetBytes(patternpage).Length.ToString & vbCrLf &
                "Connection: close" & vbCrLf &
                "Accept-Ranges: text" & vbCrLf & vbCrLf & patternpage

                Dim index_data As Byte() = iso.GetBytes(index_resp)

                Try
                    stream.Write(index_data, 0, index_data.Length)
                Catch ex As Exception

                End Try

                client.Close()
            Else
                'Ressource hardcodée ou hébergée
                WriteLog("Fichier demandé par le client: " & arg, , client)

                Dim sent_res As String = "HTTP/1.0 200 OK" & vbCrLf
                Dim sent_data As Byte()

                Select Case LCase(arg)
                    Case "cosmic.gif"
                        'L'arrière-plan dans le style Cosmic Panda.
                        sent_res &= "Content-Type: image/gif" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\cosmic.gif").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)
                        stream.Write(sent_data, 0, sent_data.Length)

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\cosmic.gif", IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                Exit Do
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        WriteLog("Ressource '" & arg & "' trouvée et envoyée! (Code HTTP 200)")
                    Case "yt_logo2.gif", "yt_logo.gif", "yt_modrn.gif", "yt_dark.gif"
                        'Les logos RetroYT qui font penser à ceux de YouTube, au format GIF pour garantir une compatibilité maximale avec les navigateurs anciens.
                        sent_res &= "Content-Type: image/gif" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\" & arg).ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)
                        stream.Write(sent_data, 0, sent_data.Length)

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\" & arg, IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                Exit Do
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        WriteLog("Ressource '" & arg & "' trouvée et envoyée! (Code HTTP 200)")
                    Case "favicon.ico"
                        'Envoi du fichier favicon.ico
                        sent_res &= "Content-Type: image/x-icon" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & FileLen(CurDir() & "\resfiles\favicon.ico").ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_res)
                        stream.Write(sent_data, 0, sent_data.Length)

                        fs = New System.IO.FileStream(CurDir() & "\resfiles\favicon.ico", IO.FileMode.Open, IO.FileAccess.Read)

                        Do
                            resread = fs.Read(resBuffer, 0, resBuffer.Length)
                            If resread = 0 Then Exit Do

                            Try
                                stream.Write(resBuffer, 0, resread)
                            Catch ex As Exception
                                Exit Do
                            End Try
                        Loop

                        fs.Close()
                        client.Close()
                        WriteLog("Ressource '" & arg & "' trouvée et envoyée! (Code HTTP 200)")
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
                            Case Else
                                sent_css &= " background-color: #ffffff;" & vbCrLf
                                sent_css &= " color: #000000;" & vbCrLf
                        End Select

                        If wanted_skin = "cosmic" Then sent_css &= " background-image: url('cosmic.gif');" & vbCrLf

                        sent_css &= " font-family: Tahoma;" & vbCrLf
                        sent_css &= " padding: 12px 12px 12px 12px;" & vbCrLf
                        sent_css &= " line-height: 18px;"
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "html, body, td, p {" & vbCrLf
                        sent_css &= " font-size: 12px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_css &= "embed {" & vbCrLf
                        sent_css &= " text-align: center;" & vbCrLf
                        sent_css &= " background-color: #000000;" & vbCrLf
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

                        sent_css &= "object {" & vbCrLf
                        sent_css &= " background-color: black;" & vbCrLf
                        sent_css &= " border-radius: 8px;" & vbCrLf
                        sent_css &= "}" & vbCrLf & vbCrLf

                        sent_res &= "Content-Type: text/css" & vbCrLf
                        sent_res &= "Connection: close" & vbCrLf
                        sent_res &= "Content-Length: " & iso.GetBytes(sent_css).Length.ToString & vbCrLf & vbCrLf
                        sent_data = iso.GetBytes(sent_css)
                        stream.Write(sent_data, 0, sent_data.Length)
                        WriteLog("Ressource '" & arg & "' envoyée! (Code HTTP 200)")
                        client.Close()
                    Case Else
                        'En cas de ressource introuvable, ou inutilisée par le serveur
                        WriteLog("Erreur 404: Ressource introuvable !")
                        Dim compiled_text As String = "<h1>404 Error - Not found</h1>" & vbCrLf &
                        "<p>Resource <i>/" & arg & "</i> was not found on this server.</p>" & vbCrLf
                        Dim notfound_resp As String =
                        "HTTP/1.0 404 Not found" & vbCrLf &
                        "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
                        "Content-Length: " & iso.GetBytes(compiled_text).Length.ToString & vbCrLf &
                        "Accept-Ranges: text" & vbCrLf & vbCrLf & compiled_text

                        Dim notfound_data As Byte() = iso.GetBytes(notfound_resp)

                        Try
                            stream.Write(notfound_data, 0, notfound_data.Length)
                        Catch ex As Exception

                        End Try

                        client.Close()
                End Select
            End If

        Else
            'Les autres requêtes entraînent une erreur 400 (requête invalide).
            WriteLog("Erreur 400: Requête erronée envoyée.", , client)
            Dim compiled_bad As String = "<h1>Error 400: Internal Server Error</h1>" & vbCrLf &
           "<p>Invalid or malformed HTTP request.</p>" & vbCrLf & vbCrLf

            Dim badresp As String =
           "HTTP/1.0 400 Bad Request" & vbCrLf &
           "Content-Type: text/html; charset=iso-8859-1" & vbCrLf &
           "Content-Length: " & iso.GetBytes(compiled_bad).Length.ToString &
           "Connection: close" & vbCrLf &
           "Accept-Ranges: text" & vbCrLf & vbCrLf & compiled_bad

            Dim baddata As Byte() = iso.GetBytes(badresp)

            Try
                stream.Write(baddata, 0, baddata.Length)
            Catch ex As Exception

            End Try

            client.Close()
        End If
    End Sub
End Module
