# Feuille de route

## 1. Muscle 2.0 (gel le 23 décembre 2026)

- [ ] Tutoriel et fichiers d'exemple.
- [ ] Brancher `tensegrite-vs-treillis` sur le Muscle corrigé :
  - remplacements C4 et C5 de `MIGRATION.md` ;
  - `analysis.py` en SI ;
  - nouvelle version de `musclepy` épinglée.
- [ ] Prévenir Jonas Feron de la correction de la rigidité tangente.
- [ ] Mettre à jour l'exemple Grasshopper du simplex, qui utilise encore les anciens GUID (voir `docs/ARCHITECTURE.md`, §4).
- [x] Au démarrage (« StartPython.NET »), vérifier que la version de `musclepy` chargée est celle qu'attend `Muscle2.gha`, et afficher sinon un avertissement clair. Fait le 26 novembre 2026 : `MusclePyVersion.Expected` et `musclepy` 2.0.1.
  - Le risque : avec une ancienne copie de `musclepy`, SRDR donne des résultats faux. Depuis le commit `edc1eee`, l'inertie est envoyée en m⁴ alors que les anciennes versions l'attendaient en mm⁴.
  - Le numéro de version ne suffit pas : il vaut toujours `2.0.0` avant et après ce changement. Il faut d'abord incrémenter la version, ou exposer un marqueur de contrat (par exemple `musclepy.UNITS = "SI"`) que le plugin vérifie.
  - Le composant propose par défaut l'environnement `muscle` (`PythonNET.cs`), qui contient aujourd'hui `musclepy` 1.1.0, la version d'origine, sans SRDR ni poutres.

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
- [ ] `nonlinear_dm` avance sans itérer l'équilibre à chaque pas : dérive d'environ 10⁻³ à 100 pas sur la corde tendue (voir `CHANGELOG.md`). Ajouter des itérations de Newton-Raphson à chaque pas. C'est nécessaire pour étudier le flambement global près d'un point limite.
- [ ] Composant Grasshopper pour `main_tangent_stiffness_stability`.
- [ ] Limite de la méthode linéaire des déplacements avec un matériau bilinéaire (câbles en traction seule, barres en compression seule).
  - `PrestressScenario` choisit le module de chaque élément dans l'état fictif où les nœuds sont bloqués, après la variation de longueur libre.
  - Un câble allongé de plus que son allongement élastique compte donc comme détendu (module nul), et l'allongement n'a aucun effet. Il en va de même pour une barre raccourcie de plus que son raccourcissement élastique.
  - Cas observé : simplex de Feron et al. 2023 à a = 1,5 kN. Un câble horizontal tendu à 0,59 kN (allongement élastique d'environ 0,33 mm), allongé de 1 mm, ne change pas le niveau a, au lieu de −237 N attendus.
  - Correctif probable : prendre le module de l'état courant (rigidité tangente), pas celui de l'état fictif.
- [ ] Écarts avec les valeurs publiées par Feron et al. 2023 (`tests/MusclePyTests/validation/test_feron2023_simplex.py`) :
  - hauteur au départ de l'essai 1, avec poids propre : 1 945,5 mm au lieu de 1 946,0 mm (fig. 2(b)) ;
  - barres à 2,1 kN par nœud supérieur : −10,87 kN au lieu de −10,80 kN (fig. 2(c)), soit 0,7 %.
  - Vérifier dans l'article si ces valeurs sont mesurées ou calculées. La fiche de lecture de `tensegrite-vs-treillis` les donne comme calculées par relaxation dynamique, à confirmer. Ensuite, expliquer l'écart. Pistes : coordonnées des nœuds (imprimées ou reconstruites), répartition du poids propre, définition du niveau a = 1,5 kN, tolérance de la relaxation dynamique utilisée dans l'article.
