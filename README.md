# Tableau de bord, historique et rapports

- Tableau de bord et rapport produit journalier utilisent une journée métier locale [00:00, lendemain 00:00), et excluent les ventes annulées.
- Les requêtes d'historique, détail, rapports et export appliquent les droits vendeur (ses ventes) / administrateur (toutes les ventes). L'annulation revalide le rôle administrateur actif en base, avec motif obligatoire.
- L'annulation complète, les mouvements inverses et le statut sont enregistrés dans une transaction SQLite; une vente déjà annulée ne peut pas l'être une seconde fois. L'historique reste intact, les totaux sont recalculés depuis les ventes validées.
- L'aperçu et l'impression utilisent `PrintDocument`; une erreur d'impression ne modifie jamais la vente.
- L'export CSV utilise UTF-8 BOM, échappement CSV et apostrophe préventive des cellules pouvant être interprétées comme formules.
- Migration de schéma version 4 ajoute vendeur, statut et informations d'annulation sans supprimer les ventes historiques.

**Intégration requise :** `SessionUtilisateur` doit être créée uniquement après une authentification réelle et transmise aux écrans et services. Ne construisez pas cette session à partir d'un choix libre de rôle côté interface. Le dépôt de départ ne contenait pas de flux de connexion, ni de création de comptes vendeurs; l'application doit brancher son authentification avant d'exposer ces écrans afin que les restrictions soient effectives.
