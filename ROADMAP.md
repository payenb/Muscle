# Feuille de route

## 1. Muscle 2.0 (gel le 23 décembre 2026)

- [ ] Tutoriel et fichiers d'exemple.
- [ ] Brancher `tensegrite-vs-treillis` sur le Muscle corrigé :
  - remplacements C4 et C5 de `MIGRATION.md` ;
  - `analysis.py` en SI ;
  - nouvelle version de `musclepy` épinglée.
- [ ] Prévenir Jonas Feron de la correction de la rigidité tangente.
- [ ] Mettre à jour l'exemple Grasshopper du simplex, qui utilise encore les anciens GUID (voir `docs/ARCHITECTURE.md`, §4).

## 2. Ajouts pour la thèse

- [ ] Câbles continus à glissement libre (sans frottement).
- [ ] Flambement global :
  - imperfection selon le premier mode ;
  - interaction entre flambement local et global (barres modélisées en plusieurs éléments poutre).
- [ ] Dynamique non linéaire avec relâchement des câbles, illustrée sur le pavillon inox.
- [ ] Comparaison avec d'autres logiciels :
  - K2Engineering, Karamba3D, Kangaroo2 ;
  - OpenSeesPy ou Abaqus ;
  - SCIA, en qualitatif.

## 3. Plus tard (Muscle 2.1)

- [ ] Classe C# `Units` qui regroupe les facteurs de conversion des composants (à tester dans Grasshopper).
- [ ] A2, A3, A6, A8 et A9 de `MIGRATION.md` (dépôt `tensegrite-vs-treillis`).
- [ ] Assemblage des matrices à chaque pas (gain de vitesse).
- [ ] Option de tolérance relative pour la DR.
- [ ] Fonction autonome de calcul des réactions.
