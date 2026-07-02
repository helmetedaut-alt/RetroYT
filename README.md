RetroYT est un projet de proxy YouTube pour les anciens navigateurs et systèmes. Il a été développé afin de permettre à des anciennes configurations de pouvoir utiliser ce service en ligne.
En effet, au courant de l'année 2025, j'ai constaté que YouTube ne fonctionnait plus sur Firefox 52.0, que j'exécute sous mon ordinateur portable fonctionnant sous Windows 2000.
Parallèlement, j'ai connu Browservice au début des années 2020. Il permet à d'anciens navigateurs d'accéder au Web moderne, selon ce principe: il envoie en temps réel un rendu Chromium à jour sous la forme de frames au format JPEG. Des éléments cliquables invisibles sont disposés pour rendre les liens accessibles, et rendant l'interaction serveur/client possible. Je tiens à dire qu'il m'a beaucoup inspiré pour ce projet. Hélas, Browservice peut faire fonctionner YouTube, et donc fonctionner sous n'importe quel navigateur à peu près à jour, mais le son n'était pas reçu par le client. Tout était lu côté serveur.

Je me disais donc, constatant ces lacunes, qu'il était temps que je lance mon propre projet, qui combine l'apparence des services en ligne actuels. Le tout en adaptant le contenu aux anciennes configurations, avec l'ergonomie des services YouTube, afin de rendre l'expérience accessible à d'anciens navigateurs qui ne sont plus pris en charge officiellement. Voire de permettre à des navigateurs qui n'étaient jamais fonctionnels de le devenir.
RetroYT fait usage du Microsoft .NET Framework 6.0, de FFMPEG, de SWFObject et de YT-DLP.
Il utilise l'encodage ISO-8859-1 pour formater les pages Web, pour garantir une compatibilité maximale avec tous les anciens navigateurs. Pour les communications entre YouTube et le proxy, en revanche, l'UTF-8 est utilisé.
Program.vb contient l'ensemble du code source utilisé dans ce projet.
Les binaires du projet (c'est-à-dire une version exécutable) se situent dans la catégorie "Release" de cette page.
Vous pouvez télécharger le ZIP, et ouvrir RetroYT.exe pour lancer le serveur proxy. Il utilise le port 80 par défaut, mais vous pouvez modifier le numéro de port en le précisant en paramètre.
Exemple: YTSrv 8080
... Pour utiliser le port 8080, au lieu du port 80.

Une connexion Internet est nécessaire pour récupérer les vidéos YouTube. FFMPEG sera utilisé pour les convertir dans l'un des formats suivants, lisibles sous certaines configurations, selon le contexte, au choix:
 * AVI (Codec vidéo MPEG-4 et audio MP3), un format assez transversal parmi les Windows anciens et nouveaux.
 * AVI (Codec vidéo MSVideo1 et audio PCM) pour les systèmes comme Windows 3.11 / NT 4.0 / 95. Dans l'absolu, il est compatible avec tous les Windows. Format assez fiable, et plutôt léger.
 * AVI (Codec vidéo Cinepak et audio PCM) qui est un codec extrêmement lent à encoder, mais plutôt léger à lire et en taille de fichiers.
 * AVI (Codec vidéo Xvid et audio MP3), qui est un codec phare des années 2000. Il supporte jusqu'à du 1080p, est assez léger, et est compatible avec des systèmes assez anciens comme Windows XP.
 * AVI (YUV, PCM) est très très lourd, et peut ne pas être lu sous tous les lecteurs. Uniquement implémenté à titre expérimental. NE PAS TENTER DE LIRE DES VIDEOS DE PLUS DE QUELQUES MINUTES.
 * AVI (Codec vidéo MJPEG, audio PCM) est universel et très facile à encoder/décoder, mais peut produire des vidéos assez lourdes.
 * MP4 (Codec vidéo H.264, audio AAC), pour les navigateurs déjà compatibles HTML5 mais incompatibles avec le YouTube moderne. Format très répandu, mais ne fonctionnera probablement pas sous d'anciens systèmes.
 * MP4 Legacy (Codec vidéo H.264, audio AAC), le même mais encore plus compatible avec les anciens systèmes, y compris Android dans ses premières versions.
 * MPEG-1 (Récent) avec le codec audio MP2, un format très universel, bien que très ancien.
 * MPEG-1 100% compatible, même codec mais avec des paramètres qui produiront des fichiers certes plus lourd, mais plus compatibles que le profil ci-dessus.
 * WMV nouveau (Codec vidéo WMV2, codec audio WMAv2) est très compatible avec Windows 98SE et plus, et plus léger que le format AVI MSVideo1, plus adapté à la lecture en local.
 * WMV ancien (Codec vidéo WMV1, codec audio WMAv1), compatible à partir de Windows 95
 * Apple QuickTime (Extension MOV, codec vidéo Cinepak, audio PCM) <- Le Cinepak met un temps fou à être encodé, mais est très compatible avec les anciens systèmes.
 * Apple QuickTime (Extension MOV, codec vidéo Sorenson SVQ1, audio MP3), pour les MacOS X à partir de 1999.
 * Apple QuickTime (Extension MOV, codec vidéo MPEG-4, audio MP2), assez compatible avec MacOS.
 * Apple QuickTime (Extension MOV, codec vidéo RPZA, audio PCM), très compatible avec les MacOS des années 90.
 * Apple QuickTime (Extension MOV, codec vidéo MJPEG, audio PCM)
 * RealMedia (Codec vidéo RV10, audio AC3) est utile pour Windows 3.11, ou Windows NT 3.51, par exemple
 * 3GP (Codec vidéo H.263, audio AMR Narrow Band) pour les anciens téléphones mobiles
 * FLV (Vidéo flash, avec un codec vidéo Sorenson Spark, audio MP3), format emblématique des débuts de YouTube, compatibilité transversale.

On peut utiliser les technologies suivantes pour intégrer les vidéos:
* Balise embed (Intégration générique)
* Lecteur Windows Media 6.4 (via ActiveX)
* Lecteur Windows Media 7.0 et plus (via ActiveX)
* Lecteur Apple QuickTime (via ActiveX)
* Lecteur Apple QuickTime (via embarcation multimédia) (Compatible MacOS)
* Lecteur VLC (via ActiveX)
* Lecteur VLC (via ActiveX avec un CLSID alternatif)
* Lecteur VLC (via embarcation multimédia) (Très compatible Linux)
* Lecteur Real Player (via ActiveX)
* Lecteur Real Player (via embarcation multimédia)
* Lecteur Flash Player (via Javascript)
* Lecteur Flash Player (ActiveX)
* Lecteur Flash Player (via embarcation multimédia)
* Objet multimédia générique (Très compatible Linux et navigateurs Mozilla)
* Intégration via la balise video de HTML5, pour les navigateurs sortis après 2008 qui le prennent en charge de façon officielle. Cette balise est universelle passé cette année.
* Intégration via la balise video de HTML5, adaptée pour les navigateurs sous Android 2.x, 3.x, 4.x et les consoles Nintendo et SONY du style Wii, WiiU, 3DS, etc. (Code fait par LeJarb en s'aidant de Léo AI)

Même si toutes ces résolutions ne sont pas disponibles dans tous les formats, la liste des résolutions disponibles est la suivante:
96p, 120p, 144p, 240p, 360p, 480p, 720p, 1080p

Je déconseille d'utiliser le proxy à travers Internet, entre le client et lui, car aucun chiffrement n'est implémenté, afin de garantir une pleine compatibilité avec les anciens navigateurs.
Je déconseille de lancer la lecture de vidéos excédant 10 minutes environ, surtout si vous lisez des vidéos sur des anciens systèmes. Analysez bien la configuration de votre système avant de lancer une vidéo longue.
Bien que les collisions des utilisations sont implémentées depuis la version Bêta 5.0, il vaut mieux utiliser ce proxy pour un seul utilisateur à titre expérimental, car ce projet n'est pas destiné à une utilisation massive.
Le proxy utilise un cookie pour mémoriser les paramètres. En son absence, les paramètres par défaut seront utilisés.
Si les paramètres par défaut ne fonctionnent pas avec votre configuration, et que la vidéo ne démarre pas, vous pouvez essayer de communiquer les paramètres de configuration dans l'URL.
Par exemple: &size=cinema&codec=mp4&player=video&resolution=auto&framerate=15 à la fin de l'URL après ?watch=id_video. Le tout est mieux expliqué dans la section "À propos" du site.

Le client doit avoir ActiveMovie ou Windows Media Player 6.4 minimum d'installé pour fonctionner, mais vous pouvez toujours lire le lien généré dans VLC, avec la fonction CTRL+N.
Toutes les versions d'Internet Explorer fonctionnent à priori, de la 1.0 à la 11.0. Veillez à adapter la configuration dans les paramètres du client.

Lorsque vous naviguez sur le proxy, la page d'index s'affiche, et vous invite à faire une recherche. Vous avez le lien "Paramètres" ou "À propos de RetroYT" pour obtenir plus d'informations.
Les recherches et la conversion des vidéos sont un peu lentes, le chargement prend environ 20 secondes par recherche.
La conversion puis l'envoi de la vidéo vers le client peuvent prendre du temps, surtout si vous tentez de visualiser une vidéo longue.
Néanmoins, j'ai pu regarder des vidéos YouTube depuis un système Windows NT 4.0, avec Windows Media Player 6.4 d'installé, sur Internet Explorer 6.0.
Windows Media Player 6.0 et moins ne prennent pas en charge les liens vers les URL, et Internet Explorer 2.0 et moins ne prennent pas en charge l'intégration d'objets multimédia au sein des pages Web.

On peut aussi changer l'apparence du site avec un système de thèmes (Classic, Cosmic Tube, Modern, Dark Mode, Rose, Aqua, Mint et Monochrome).
La taille automatique du lecteur est disponible, elle fait usage du Javascript, mais peut ne pas fonctionner de façon certaine sous de très anciennes versions d'IE.
Je conseille donc de laisser la taille du lecteur en 640x480 pour la plupart des cas.
Trois nouvelles tailles de lecteurs intégrés dans les pages Web ont été ajoutées dans la Bêta 7.0: 800x600, 1024x768, et 1600x1200.

En pratique, la configuration minimale conseillée pour la lecture dans le navigateur est la suivante:
* Ecran VGA en 800x600 en 16-bits de couleurs
* Microsoft Windows 95
* Microsoft Internet Explorer 3.0
* Windows Media Player 6.4 ou ActiveMovie
* Flash Player 7
* 64Mo de RAM
* 4Mo de VRAM
* Quelques centaines de Mo d'espace disque, j'imagine?
* Processeur Intel Pentium I ou équivalents

Il est possible de naviguer sur le site avec Internet Explorer 1.0 et 2.0 mais l'intégration OLE n'est pas disponible, donc le lecteur n'apparaîtra à priori pas dans la page Web.
J'ai déjà eu des feedbacks sur le fait que Flash Player 7 fonctionne, et qu'on peut lire aussi des vidéos de façon intégrée sous Windows 3.11, mais il s'agit de cas exceptionnels peu documentés.
Pareil pour le projet Arachne, il semble que son usage est possible, et que la lecture soit disponible sur un lecteur MS-DOS externe, mais je n'ai eu qu'un seul feedback là-dessus.
Lire sous de vieilles versions de MacOS est faisable. Pareil sous BeOS, OS/2 Warp, et Linux. Cela est faisable également, selon des témoignages et mes propres expériences.

La version 3.x ajoute à nouveau des fonctions de streaming direct via VLC, par exemple (http://serveur/stream?v=identifiant) pour lire depuis VLC sans interface Web. Il existe aussi une fonction de recherche immédiate, avec lecture de la première vidéo trouvée (http://serveur/lucky?q=motclef). Cela permet de lire une vidéo YouTube depuis un lecteur VLC sans connaître l'identifiant, ni faire usage de portail Web, mais en choisissant une vidéo avec un mot-clef précis.

La version 5.x ajoute le volet de suggestions, à droite des vidéos (entre autres).

La version 6.0 ajoute la navigation sur les chaînes, l'affichage des commentaires, et un simulacre de flux principal sur la page d'index. Il est désactivé par défaut, car il lance une recherche qui peut rendre le chargement plus long.

La version 8.0 apporte des corrections majeures, une mise à jour de l'interface serveur et client, et la prise en charge partielle des émojis.

La version 9.0 apporte les volets droits pour les listes de lecture, implémente les Shorts (avec un volet droit aussi, naviguer sur /short est désormais possible) et quelques boutons de navigation, ainsi que des corrections de bugs et d'affichage. Il y a aussi la section "lives" qui a été ajoutée. Dans le code, certaines fonctions ont été factorisées pour être plus optimisées. Dans les paramètres, on peut choisir la taille du lecteur vertical. En revanche, certains codecs sont indisponibles pour lire des vidéos verticales (comme MSVideo1, Cinepak, et le MPEG-1 dit "100% compatible"). Pour les très anciennes configurations, l'usage de MPEG-1 (dans sa version "récente") ou de RM sont conseillés, à condition d'installer les codecs et logiciels nécessaires (comme Real Player 5.0 sous Windows 3.11).

Une page de débug est disponible en naviguant sur http://serveur/debug.cgi.

Une page pour consulter les vidéos en cache est disponible sur http://serveur/cache.cgi mais seules les IP locales peuvent y accéder, pour des raisons de confidentialité.

Pour naviguer sur une playlist, vous pouvez naviguer d'abord sur une chaîne avec channel.cgi?id=UCxxxxx, puis parmi les trois sections (vidéos, shorts, et playlists), veuillez cliquer sur playlists.

Ceci mènera vers playlist.cgi?id=PLxxxxxx. Vous pouvez également ajouter manuellement l'identifiant au bout de l'URL.

Ce logiciel est livré sans garantie.

En espérant qu'il vous procurera entière satisfaction.

Monokeros
