RetroYT est un projet de proxy YouTube pour les anciens navigateurs et systèmes.
En effet, l'an passé, j'ai constaté que YouTube ne fonctionnait plus sur Firefox 52.0 sous mon Windows 2000.
J'ai connu le projet Browservice au début des années 2020, et il m'a beaucoup inspiré pour ce projet.
Je me disais donc qu'il était temps que je lance mon propre projet, qui combine l'apparence des services en ligne actuels, tout en adaptant le contenu aux anciennes configurations, avec l'ergonomie des services YouTube, afin de rendre l'expérience accessible à d'anciens navigateurs.
RetroYT fait usage du framework Microsoft .NET 6.0, FFMPEG, SWFObject et YT-DLP.
Il utilise l'encodage ISO-8859-1, pour garantir une compatibilité maximale avec tous les anciens navigateurs. Entre YouTube et le proxy, en revanche, UTF-8 est utilisé.
Program.vb contient l'ensemble du code source utilisé dans ce projet.
Les binaires du projet (c'est-à-dire une version exécutable) se situent dans la catégorie Release de cette page.
Vous pouvez télécharger le ZIP, et ouvrir RetroYT.exe pour lancer le serveur proxy. Il utilise le port 80 par défaut, mais vous pouvez modifier le numéro de port en le précisant en paramètre.
Exemple: YTSrv 8080
... Pour utiliser le port 8080, au lieu du port 80.

Une connexion Internet est nécessaire pour récupérer les vidéos. FFMPEG sera utilisé pour les convertir dans l'un des formats suivants, lisibles sous certaines configurations, selon le contexte:
 * AVI (Codec vidéo MPEG-4 et audio MP3), un format assez transversal parmi les Windows anciens et nouveaux.
 * AVI (Codec vidéo MSVideo1 et audio PCM) pour les systèmes comme Windows 3.11 / NT 4.0 / 95. Dans l'absolu, il est compatible avec tous les Windows.
 * AVI (Codec vidéo Cinepak et audio PCM) qui est un codec extrêmement lent à encoder, mais plutôt léger à lire et en taille de fichiers
 * AVI (YUV, PCM)
 * MP4 original, avec un paramètre pour forcer le codec vidéo H.264 et audio M4A, pour les navigateurs déjà compatibles HTML5 mais incompatibles avec le YouTube moderne.
 * MPEG-1 avec le codec audio MP2
 * WMV (Codec vidéo WMV2, codec audio WMAv2, très compatible avec Windows 98SE et plus, et plus léger que le format AVI MSVideo1)
 * WMV (Codec vidéo WMV1, codec audio WMAv1, compatible à partir de Windows 95)
 * Apple QuickTime (Extension MOV, codec vidéo Cinepak, audio PCM)
 * Apple QuickTime (Extension MOV, codec vidéo Sorenson SVQ1, audio MP3)
 * Apple QuickTime (Extension MOV, codec vidéo MPEG-4, audio MP2)
 * Apple QuickTime (Extension MOV, codec vidéo RPZA, audio PCM)
 * RealMedia (Codec vidéo RV10, audio AC3)
 * 3GP (Codec vidéo H.263, audio AMR Narrow Band)
 * FLV (Vidéo flash, avec un codec vidéo Sorenson Spark, audio MP3)

On peut utiliser les technologies suivantes pour intégrer les vidéos:
* Balise embed (Intégration générique)
* Lecteur Windows Media 6.4 (via ActiveX)
* Lecteur Windows Media 7.0 et plus (via ActiveX)
* Lecteur Apple QuickTime (via ActiveX)
* Lecteur Apple QuickTime (via embarcation multimédia) (Compatible MacOS)
* Lecteur VLC (via ActiveX)
* Lecteur VLC (via ActiveX avec CLSID alternatif)
* Lecteur VLC (via embarcation multimédia) (Très compatible Linux)
* Lecteur Real Player (via ActiveX)
* Lecteur Real Player (via embarcation multimédia)
* Lecteur Flash Player (via Javascript)
* Lecteur Flash Player (ActiveX)
* Lecteur Flash Player (via embarcation multimédia)
* Objet multimédia générique
* Intégration via la balise video de HTML5

Même si toutes ces résolutions ne sont pas disponibles dans tous les formats, la liste des résolutions disponibles est la suivante:
96p, 120p, 144p, 240p, 360p, 480p, 720p, 1080p

Je déconseille d'utiliser le proxy à travers Internet, entre le client et lui, car aucun chiffrement n'est implémenté, afin de garantir une pleine compatibilité avec les anciens navigateurs.
Je déconseille de lancer la lecture de vidéos excédant 10 minutes environ, surtout si vous lisez des vidéos sur des anciens systèmes. Analysez bien la configuration de votre système avant de lancer une vidéo longue.
Il vaut mieux utiliser ce proxy pour un seul utilisateur à titre expérimental, car ce projet n'est pas destiné à une utilisation massive.
Le proxy utilise un cookie pour mémoriser les paramètres. En son absence, les paramètres par défaut seront utilisés.
Si ces paramètres ne fonctionnent pas avec votre configuration, et que la vidéo ne démarre pas, vous pouvez essayer de cliquer sur un des liens sous le lecteur vidéo.
Il y en a deux: un pour télécharger la vidéo directement (ou l'ouvrir dans un logiciel externe), et un pour forcer la rétrocompatibilité maximale (lecteur en 320x240, codec MSVideo1, intégration WMP 6.4, etc.)

Le client doit avoir Windows Media Player 6.4 minimum d'installé pour fonctionner, mais vous pouvez toujours lire le lien généré dans VLC, avec la fonction CTRL+N.
Toutes les versions d'Internet Explorer fonctionnent à priori, de la 1.0 à la 11.0. Veillez à adapter la configuration dans les paramètres du client.

Lorsque vous naviguez sur le proxy, la page d'index s'affiche, et vous invite à faire une recherche. Vous avez le lien "Paramètres" ou "À propos de RetroYT" pour obtenir plus d'informations.
Les recherches et la conversion des vidéos sont un peu lentes, le chargement prend environ 20 secondes par recherche.
La conversion puis l'envoi de la vidéo vers le client peuvent prendre du temps, surtout si vous tentez de visualiser une vidéo longue.
Néanmoins, j'ai pu regarder des vidéos YouTube depuis un système Windows NT 4.0, avec Windows Media Player 6.4 d'installé, sur Internet Explorer 6.0.
Windows Media Player 6.0 et moins ne prennent pas en charge les liens vers les URL, et Internet Explorer 3.0 et moins ne prennent pas en charge l'intégration d'objets multimédia au sein des pages Web.

On peut aussi changer l'apparence du site avec un système de thèmes (Classic, Cosmic Tube, Modern, ou Dark Mode).
La taille automatique du lecteur est disponible, elle fait usage du Javascript, mais force le format 4:3 et peut ne pas fonctionner sous de très anciennes versions d'IE.
Je conseille donc de laisser la taille du lecteur en 640x480 pour la plupart des cas.

En pratique, la configuration minimale conseillée pour la lecture dans le navigateur est la suivante:
* Ecran VGA en 800x600 en 16-bits de couleurs
* Microsoft Windows 95
* Microsoft Internet Explorer 4.0
* Windows Media Player 6.4
* Flash Player 8
* 64Mo de RAM
* 4Mo de VRAM
* Quelques centaines de Mo d'espace disque, j'imagine?
* Processeur Intel Pentium I ou équivalents

Il est possible de naviguer sur le site avec Internet Explorer 1.0, 2.0, et 3.0 mais l'intégration OLE est difficile voire impossible.
J'ai déjà eu des feedbacks sur le fait que Flash Player 7 fonctionne, et qu'on peut lire aussi sous Windows 3.11, mais il s'agit de cas exceptionnels peu documentés.
Naviguer sur le site avec Arachne serait possible sur MS-DOS, mais dans ces cas-là, il faudrait télécharger la vidéo PUIS l'ouvrir sur un lecteur externe.
L'expérience YouTube sera donc en deux temps, comme dans les années 90 où chaque logiciel avait son but bien paramétré en amont, et aucune intégration/interaction directe.

Ce logiciel est livré sans garantie.

En espérant qu'il vous procurera entière satisfaction.

Monokeros
