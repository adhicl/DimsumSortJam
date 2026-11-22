# Missing frozen art

Dishes that currently **never freeze** in game because they have no matching frozen picture
(32 of 79, as of 2026-11-22). Frozen art must show the **same pieces** as the normal sprite:
same count and arrangement, only the plate may be left out.

When a picture is ready: put `JAJANAN FROZEN - <dish>.png` in `Assets/Sprites/JAJANAN FROZEN/`,
slot it into the same index of `dimsumFrozenSprite` in `Assets/ScriptObjects/Installers/Setting.asset`,
and set its Pixels Per Unit so it renders the same size as the normal sprite (see the Art part of
"Frozen and hidden jajanan" in `next_step.md`).

## A. No frozen art at all (5)

| Normal image | Dish | Frozen picture needed |
|---|---|---|
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20PUTRI%20MANDI%201.png" width="96"> | `PUTRI MANDI 1` | single piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20ONDE%20ONDE%201%20-%20new%20.png" width="96"> | `ONDE ONDE 1 - new ` | single piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20CUCUR%201%20-%20new.png" width="96"> | `KUE CUCUR 1 - new` | single piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20LUMPANG%201%20-%20coklat.png" width="96"> | `KUE LUMPANG 1 - coklat` | single piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20LUMPANG%201%20-%20hijau.png" width="96"> | `KUE LUMPANG 1 - hijau` | single piece |

## B. Single piece, but the only frozen art shows several pieces (9)

| Normal image | Dish | Existing frozen art (not used) | Frozen picture needed |
|---|---|---|---|
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20COKLAT%201.png" width="96"> | `BOLU KUKUS COKLAT 1` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` | 1 cup |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20WARNA%201%20-%20hijau.png" width="96"> | `BOLU KUKUS WARNA 1 - hijau` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` | 1 cup |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20WARNA%201%20-%20kuning.png" width="96"> | `BOLU KUKUS WARNA 1 - kuning` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` | 1 cup |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20WARNA%201%20-%20pink.png" width="96"> | `BOLU KUKUS WARNA 1 - pink` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` | 1 cup |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20MANGKOK%201%20-%20hijau.png" width="96"> | `KUE MANGKOK 1 - hijau` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20MANGKOK%201.png" width="96"><br>`MANGKOK 1` | 1 piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20MANGKOK%201%20-%20kuning.png" width="96"> | `KUE MANGKOK 1 - kuning` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20MANGKOK%201.png" width="96"><br>`MANGKOK 1` | 1 piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20MANGKOK%201%20-%20pink.png" width="96"> | `KUE MANGKOK 1 - pink` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20MANGKOK%201.png" width="96"><br>`MANGKOK 1` | 1 piece |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20KUKUS%20COKLAT%201.png" width="96"> | `KUE KUKUS COKLAT 1` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20KUKUS%20KOTAK%201.png" width="96"><br>`KUKUS KOTAK 1` | 1 slab |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20KUKUS%20PANDAN%201.png" width="96"> | `KUE KUKUS PANDAN 1` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20KUKUS%20KOTAK%201.png" width="96"><br>`KUKUS KOTAK 1` | 1 slab |

## C. Plate sprite, but the frozen art shows different pieces (15)

| Normal image | Dish | Existing frozen art (not used) | Frozen picture needed |
|---|---|---|---|
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KOJO%203%20-%20plate%20-%20new.png" width="96"> | `BOLU KOJO 3 - plate - new` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KOJO%201.png" width="96"><br>`BOLU KOJO 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20LAPIS%20SURABAYA%202%20-%20plate%20-%20new%202.png" width="96"> | `LAPIS SURABAYA 2 - plate - new 2` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20LAPIS%20SURABAYA%201.png" width="96"><br>`LAPIS SURABAYA 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20LAPIS%20TAPIOKA%203%20-%20plate.png" width="96"> | `LAPIS TAPIOKA 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20LAPIS%20TAPIOKA%201.png" width="96"><br>`LAPIS TAPIOKA 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20LAPIS%20TAPIOKA%203%20HIJAU%20-%20plate.png" width="96"> | `LAPIS TAPIOKA 3 HIJAU - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20LAPIS%20TAPIOKA%201.png" width="96"><br>`LAPIS TAPIOKA 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20LEMPER%203%20-%20plate.png" width="96"> | `LEMPER 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20LEMPER%201.png" width="96"><br>`LEMPER 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20RISOL%203%20-%20plate%20-%20new.png" width="96"> | `RISOL 3 - plate - new` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20RISOL%201.png" width="96"><br>`RISOL 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20SEMAR%20MENDEM%202%20-%20plate.png" width="96"> | `SEMAR MENDEM 2 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20SEMAR%20MENDEM%201.png" width="96"><br>`SEMAR MENDEM 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20WINGKO%203%20-%20plate%20-%20new.png" width="96"> | `WINGKO 3 - plate - new` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20WINGKO%201.png" width="96"><br>`WINGKO 1` | plate arrangement |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20DADAR%20GULUNG%203%20-%20plate.png" width="96"> | `DADAR GULUNG 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20DADAR%20GULUNG%201.png" width="96"><br>`DADAR GULUNG 1` | 3 rolls |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20GEMBLONG%203%20-%20plate.png" width="96"> | `GEMBLONG 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20GEMBLONG%201.png" width="96"><br>`GEMBLONG 1` | the pile |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20DELAPAN%20JAM%203%20-%20plate%20-%20new.png" width="96"> | `KUE DELAPAN JAM 3 - plate - new` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20KUE%208%20Jam%201.png" width="96"><br>`KUE 8 Jam 1` | 3 slices |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20KUE%20LAPIS%203%20-%20plate.png" width="96"> | `KUE LAPIS 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20KUE%20LAPIS%201.png" width="96"><br>`KUE LAPIS 1` | 3 pieces |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20WAJIK%203%20-%20plate.png" width="96"> | `WAJIK 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20WAJIK%201.png" width="96"><br>`WAJIK 1` | 3 pieces |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20PUKIS%203%20-%20plate.png" width="96"> | `PUKIS 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20PUKIS%201.png" width="96"><br>`PUKIS 1` | 3 pieces |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20GETUK%203%20-%20plate.png" width="96"> | `GETUK 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20GETUK%201.png" width="96"><br>`GETUK 1` | the rolled pink-striped pieces |

## Possible quick wins: no new art needed (3)

These were disabled with the rest, but the existing frozen art looks like a match. Re-enable by
pointing the entry back at it (and checking its PPU) if they pass a look.

| Normal image | Dish | Existing frozen art |
|---|---|---|
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20COKLAT%203%20-%20plate.png" width="96"> | `BOLU KUKUS COKLAT 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20BOLU%20KUKUS%20WARNA%203%20-%20plate.png" width="96"> | `BOLU KUKUS WARNA 3 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20BOLU%20KUKUS%201.png" width="96"><br>`BOLU KUKUS 1` |
| <img src="Assets/Sprites/Jajanan/JAJANAN%20-%20GETUK%20warna%204%20-%20plate.png" width="96"> | `GETUK warna 4 - plate` | <img src="Assets/Sprites/JAJANAN%20FROZEN/JAJANAN%20FROZEN%20-%20GETUK%201.png" width="96"><br>`GETUK 1` |
