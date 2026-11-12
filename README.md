# Livraison x64 et sauvegardes

## Publication autonome Windows x64

1. Installer le SDK .NET 10 sur la machine de build et Inno Setup 6.
2. Exécuter `installer/publish-win-x64.cmd`.
3. Ouvrir `installer/GestionMagasin.iss` dans Inno Setup puis compiler. L'installeur est généré dans `artifacts/installer`.

La publication `win-x64 --self-contained` embarque le runtime .NET : Visual Studio et le runtime .NET ne sont pas requis sur le poste client. Le fichier SQLite reste dans `%LOCALAPPDATA%` et n'est pas installé sous Program Files.

## Backups

`SqliteBackupService` utilise `SqliteConnection.BackupDatabase`, vérifie `integrity_check`, la version du schéma et les clés étrangères. Les restaurations passent par une copie temporaire validée et un backup pré-restauration. Les backups automatiques démarrent au lancement et s'effectuent une fois par jour tant que l'application reste ouverte; seuls les sept derniers automatiques sont gardés. Un backup manuel/restauration doit être exposé dans une interface d'administration authentifiée avant livraison.

## Limites de recette à traiter

La liste des cas est dans `docs/UAT.md`. Les statuts sont « À tester »: aucune exécution/build Windows n'a été effectuée dans cette modification. Les contraintes <2 s et les 18 cas doivent être mesurés sur une machine cible et une base représentative. Le dépôt doit aussi brancher la session authentifiée au service de vente pour attribuer `VendeurId`; les anciens enregistrements restent NULL jusqu'à une attribution explicite.
