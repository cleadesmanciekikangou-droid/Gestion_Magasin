# Gestion Magasin — Ventes

Le module « Nouvelle vente » permet de rechercher des produits actifs, de constituer un panier (les ajouts d'un même produit sont fusionnés), de modifier ou retirer une ligne et de choisir le paiement en espèces, Mobile Money ou carte.

La validation est effectuée dans une transaction SQLite immédiate : les produits, leur état actif, leur prix et leur stock sont relus avant toute écriture. Si un prix a changé ou si le stock est insuffisant, la transaction est annulée. Les lignes enregistrent les snapshots référence/nom/prix; les mouvements de sortie sont créés dans la même transaction.

Chaque tentative possède un `JetonValidation` GUID unique stocké avec contrainte UNIQUE. Une répétition avec le même jeton retourne la vente déjà créée au lieu de la dupliquer. Le bouton de validation est désactivé pendant l'opération. En espèces, le montant insuffisant est bloqué et la monnaie est calculée; pour les paiements électroniques, le montant est fixé au total.

La migration de schéma version 3 crée `Vente` et `LigneVente` et relie les mouvements à la vente sans supprimer les données des versions précédentes.

Lancer sous Windows avec .NET 10 : `dotnet build Gestion_Magasin.sln`, puis `dotnet run --project src/GestionMagasin.WinForms`.
