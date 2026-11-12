# Gestion Magasin — Catalogue et stocks

Application WinForms en couches (.NET 10 LTS): modèles, services métier, accès SQLite et interface. Le fichier SQLite est stocké sous `%LOCALAPPDATA%/GestionMagasin/gestion-magasin.db`.

## Catalogue

- Recherche par référence ou nom, filtre catégorie et statut actif/archivé, colonnes triables.
- Création/modification de produits et catégories. L'archivage désactive la vente sans supprimer le produit ni ses mouvements.
- Le stock affiché est une somme dynamique des quantités signées de `MouvementStock`; aucun champ de quantité n'est modifiable directement.

## Mouvements et règles de stock

- Stock initial et réapprovisionnement : mouvements positifs.
- Vente validée : mouvement négatif; refusée si produit archivé ou stock insuffisant.
- Annulation de vente : mouvement positif lié à la vente originale, au plus une fois.
- Correction : mouvement égal à l'écart exact entre stock compté et stock théorique.
- Les écritures sont transactionnelles et sérialisées. Toute sortie est contrôlée contre le stock disponible.
- Seuil : 0 = rouge « En rupture »; 0 < stock <= seuil = orange « Stock faible »; au-dessus = vert « Disponible ».

L'initialisation applique automatiquement la migration version 2 sans effacer les données existantes. La validation d'une vente et son annulation sont exposées par les services pour être appelées par le futur module de caisse.

## Build

Sur Windows avec le SDK .NET 10 : `dotnet build Gestion_Magasin.sln`; puis `dotnet run --project src/GestionMagasin.WinForms`.
