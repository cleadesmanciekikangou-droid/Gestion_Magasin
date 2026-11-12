# UAT – vérification avant recette

| ID | Cas à exécuter | Résultat attendu | Statut |
|---|---|---|---|
| T01 | Initialisation/migration d'une base vierge | Schéma à jour, données conservées | À tester |
| T02 | Créer produit/catégorie puis rechercher | Résultat exact, recherche fluide | À tester |
| T03 | Vente valide avec stock disponible | Vente + lignes + mouvements atomiques | À tester |
| T04 | Vente supérieure au stock | Refus sans écriture partielle | À tester |
| T05 | Deux validations concurrentes au dernier article | Une seule réussite | À tester |
| T06 | Rejouer le même JetonValidation | Pas de vente dupliquée | À tester |
| T07 | Produit archivé dans un panier | Validation refusée | À tester |
| T08 | Prix modifié après ajout au panier | Validation refusée, panier à actualiser | À tester |
| T09 | Paiement espèces insuffisant | Refus; aucune vente | À tester |
| T10 | Paiement électronique | Exactement le total, sans monnaie | À tester |
| T11 | Annulation vente par vendeur | Refusée côté service et dépôt | À tester |
| T12 | Annulation admin avec motif | Statut annulé et mouvements inverses atomiques | À tester |
| T13 | Double annulation | Refusée sans second mouvement | À tester |
| T14 | Désactivation du dernier admin | Refusée atomiquement | À tester |
| T15 | Rapport jour local aux frontières minuit | Intervalle [00:00, lendemain 00:00) | À tester |
| T16 | Backup/restore avec fichier intègre et corrompu | Restore valide ou conservation intégrale de la base précédente | À tester |
| T17 | CSV contenant =, +, -, @ ou tabulation | Cellule protégée, CSV correctement échappé | À tester |
| T18 | Jeu volumineux 5 000 produits / 50 000 ventes | Recherche et validation <2 s sur machine cible | À tester |

**Important :** ces cas ne sont pas déclarés passés. La performance, la concurrence et les tests UAT nécessitent d'exécuter l'application et une base synthétique sur Windows. Le suivi des ventes par utilisateur nécessite également de transmettre l'identifiant authentifié du vendeur au checkout; vérifier ce câblage avant de valider T11–T12 et les statistiques par rôle.
