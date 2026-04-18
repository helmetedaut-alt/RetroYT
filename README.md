RetroYT est un projet de proxy YouTube pour les anciens navigateurs et systèmes.
En effet, l'an passé, j'ai constaté que YouTube ne fonctionnait plus sur Firefox 52.0 sous mon Windows 2000.
J'ai connu le projet Browservice au début des années 2020, et il m'a beaucoup inspiré pour ce projet.
Je me disais donc qu'il était temps que je lance mon propre projet, qui combine l'apparence des services en ligne actuels, tout en adaptant le contenu aux anciennes configurations, avec l'ergonomie des services YouTube, afin de rendre l'expérience accessible à d'anciens navigateurs.
RetroYT fait usage du framework Microsoft .NET 6.0, FFMPEG et YT-DLP.
Il utilise l'encodage ISO-8859-1, pour garantir une compatibilité maximale avec tous les anciens navigateurs. Entre YouTube et le proxy, en revanche, UTF-8 est utilisé.
Program.vb contient l'ensemble du code source utilisé dans ce projet.
Les binaires du projet (c'est-à-dire une version exécutable) se situent dans la catégorie Release de cette page.
Vous pouvez télécharger le ZIP, et ouvrir RetroYT.exe pour lancer le serveur proxy. Il utilise le port 80 par défaut, mais vous pouvez modifier le numéro de port en le précisant en paramètre.
Exemple: YTSrv 8080
... Pour utiliser le port 8080, au lieu du port 80.

Une connexion Internet est nécessaire pour récupérer les vidéos. FFMPEG sera utilisé pour les convertir dans l'un des formats suivants, lisibles sous certaines configurations, selon le contexte:
 * AVI (MPEG4) au format 480p, un format assez transversal parmi les Windows anciens et nouveaux.
 * AVI (MSVideo1) pour les systèmes comme Windows NT 4.0 / 95. Il est compatible avec tous les Windows en pratique. Même s'il est en 320x240, il peut rapidement s'avérer très lourd.
 * MP4 original (dans sa version originale, pour les navigateurs déjà compatibles HTML5 mais incompatibles avec le YouTube moderne)

Je déconseille d'utiliser le proxy à travers Internet, entre le client et lui, car aucun chiffrement n'est implémenté, afin de garantir une pleine compatibilité avec les anciens navigateurs.
Je déconseille de lancer la lecture de vidéos excédant 10 minutes environ, surtout si vous choisissez le codec MSVIDEO1, très compatible avec les anciens Windows, mais très lourd. Analysez bien la configuration de votre système avant de lancer une vidéo longue.
Il vaut mieux utiliser ce proxy pour un seul utilisateur à titre expérimental, car ce projet n'est pas destiné à une utilisation massive.
Le proxy utilise un cookie pour mémoriser les paramètres. En son absence, les paramètres par défaut seront utilisés.
Si ces paramètres ne fonctionnent pas avec votre configuration, et que la vidéo ne démarre pas, vous pouvez essayer de cliquer sur un des liens sous le lecteur vidéo.
Il y en a deux: un pour télécharger la vidéo directement (ou l'ouvrir dans un logiciel externe), et un pour forcer la rétrocompatibilité maximale (lecteur en 320x240, codec MSVideo1, etc.)

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

En pratique, la configuration minimale pour la lecture dans le navigateur est la suivante:
* Ecran VGA en 800x600 en 16-bits de couleurs
* Microsoft Windows 95
* Microsoft Internet Explorer 4.0
* Windows Media Player 6.4
* 64Mo de RAM
* 2Mo de VRAM
* Quelques centaines de Mo d'espace disque, j'imagine?
* Processeur Intel Pentium I ou équivalents

Il est possible de naviguer sur le site avec Internet Explorer 1.0, 2.0, et 3.0 mais l'intégration OLE est pratiquement impossible.
Naviguer sur le site avec Arachne serait possible sur MS-DOS, mais dans ces cas-là, il faudrait télécharger la vidéo et l'ouvrir sur un lecteur externe.
L'expérience YouTube sera donc en deux temps, comme dans les années 90 où chaque logiciel avait son but bien paramétré en amont, et aucune intégration/interaction directe.

Ce logiciel est livré sans garantie.

En espérant qu'il vous procurera entière satisfaction.

Monokeros
