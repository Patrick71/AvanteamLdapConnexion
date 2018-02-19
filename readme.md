=Tester la connexion LDAP=
L'exécutable permet de se connecter à un serveur LDAP et affiche dans la console les attributs demandés suivant le filtre spécifié.

==Configuration==
Ouvrir le fichier AvanteamLdapConnexion.exe.config et renseigner les clés:
- Path
- Filter
- Attributs

==Ligne de commande==

> AvanteamLdapConnexion.exe -l pchaumeil -p avanteam

ou pour une connexion anonyme
> AvanteamLdapConnexion.exe