# Gestion Magasin — Cosmétiques

Squelette initial d'une application Windows Forms en C# avec une architecture en couches et une base SQLite locale.

## Prérequis et lancement

- .NET 10 SDK (Windows Desktop Runtime pour exécuter l'application)
- Depuis Windows : `dotnet build Gestion_Magasin.sln`
- Lancer `src/GestionMagasin.WinForms` avec `dotnet run --project src/GestionMagasin.WinForms`

La première ouverture applique les migrations et impose la création d'un administrateur. Aucun identifiant ni mot de passe par défaut n'est créé. La fenêtre principale constitue le point de départ pour les écrans métier à venir.

## Architecture

- `GestionMagasin.Models` : modèles métier partagés.
- `GestionMagasin.Data` : fabrique de connexions, migrations versionnées et dépôt SQLite.
- `GestionMagasin.Services` : règles métier et hachage sécurisé des mots de passe.
- `GestionMagasin.WinForms` : composition de l'application et interface utilisateur.

La base est créée dans `%LOCALAPPDATA%/GestionMagasin/gestion-magasin.db`, hors du répertoire d'installation. Les migrations sont suivies dans `VersionSchema` et par `PRAGMA user_version`. Les clés étrangères sont activées sur chaque connexion.

Les mots de passe sont dérivés avec PBKDF2-HMAC-SHA256 (600 000 itérations), sel aléatoire unique de 16 octets et empreinte de 32 octets. Les paramètres sont enregistrés pour permettre une évolution ultérieure. Les requêtes SQL utilisent des paramètres.
