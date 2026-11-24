# Tests de non-régression de musclepy

Ces tests figent les résultats actuels de `musclepy` : efforts normaux, déplacements, coordonnées, réactions, fréquences propres, moments. Ils échouent dès qu'une modification du code (refactorisation, packaging, mise à jour de numpy ou scipy…) change un résultat au-delà d'une tolérance négligeable pour l'ingénieur.

Ils ne disent pas si les résultats sont *justes* : c'est le rôle des tests de `tests/MusclePyTests`, qui comparent à des solutions analytiques ou expérimentales. Ils disent seulement si les résultats ont *changé*.

## Lancer

Depuis la racine du dépôt, avec l'environnement conda `muscle-dev` :

```bash
python -m pytest                    # toute la suite (tests unitaires + régression)
python -m pytest tests/regression   # seulement la régression
```

`conftest.py` place `src/MusclePy` en tête du `sys.path`. On teste donc toujours le code du dépôt, même si un autre `musclepy` est installé dans l'environnement. L'en-tête de pytest affiche la version et l'emplacement du `musclepy` utilisé.

## Ajouter ou mettre à jour une référence

1. Ajouter une fonction décorée par `@case` dans `regression_cases.py`. Elle renvoie `{"results": {...}, "info": {...}}`.
2. Générer les références : `python -m pytest tests/regression --update-references`.
3. Relire le diff des fichiers `tests/reference/*.json` et le commiter.

On ne régénère **pas** une référence pour faire passer un test. On la régénère seulement si le changement de résultat est voulu (correction d'un bug, nouvelle méthode), et on l'explique dans le message de commit.

## Ce qui est comparé

Chaque fichier `tests/reference/<cas>.json` contient :

- les entrées `results`, comparées valeur par valeur ;
- les entrées `info` (nombre d'itérations, convergence), notées pour contexte mais **non comparées** ;
- les versions de musclepy, numpy, scipy et Python utilisées pour générer le fichier.

Tolérance pour chaque tableau : `|calculé − référence| ≤ 1e-5·|référence| + 1e-6·max|référence| + 1e-9`.
Cela représente 0,01 N sur 1 kN. Les résultats varient d'environ 1e-13 (en relatif) d'une exécution à l'autre, à cause du BLAS multithreadé : une comparaison bit à bit est donc impossible.

Deux cas particuliers :

- **SVD** : les bases de modes d'autocontrainte et de mécanismes ne sont pas uniques (signe, rotation dans un sous-espace). On compare donc les projecteurs sur ces sous-espaces, qui sont uniques.
- **Modes propres** : normés et orientés (plus grande composante positive). Si une fréquence est répétée, le mode n'est pas défini : il est stocké en `null` et n'est pas comparé.

## Cas couverts

| Cas | Solveur | Origine |
|---|---|---|
| `simplex_dr_cable_shortening` | DR | Simplex expérimental (Feron et al. 2023), `examples/Tensegrity Simplex` |
| `simplex_srdr_default_imperfection` | SRDR | Même simplex, imperfection L0/300 (Payen et al. 2026) |
| `simplex_svd` | SVD | Même simplex |
| `simplex_dynamic_lumped_mass`, `simplex_dynamic_consistent_mass` | DR + analyse modale | Essais dynamiques du simplex |
| `two_cables_dr_loading`, `three_cables_dr_*` | DR | Cas analytiques de `tests/MusclePyTests/solvers/dr` |
| `two_bars_linear_dm`, `two_bars_nonlinear_dm` | DM linéaire / non linéaire | Mémoire J. Feron, annexe A1.1.1 |
| `tight_rope_nonlinear_dm_3_stages` | DM | Corde tendue précontrainte, 3 étapes |
| `snelson_2x_localized_self_stress` | SVD + localisation | Aloui et al. 2019 |
| `feron2023_simplex_test_1` | SVD, DM linéaire, DR | Simplex de Feron et al. 2023 avec les données publiées : mode, sensibilité, essai 1 (voir aussi `tests/MusclePyTests/validation`) |
| `beam_fem_*`, `beam_dr_cantilever_tip_load` | Poutre NR / DR | Cas de `tests/MuscleCoreTests/Solvers/Beam*` |

Les fichiers `.gh` (`examples/`, `TestsGH/`) ne peuvent pas être exécutés sans Rhino. Les cas ci-dessus reproduisent leurs structures directement en Python, au niveau de l'API que le plugin appelle.
