# Archived art

`KayKit/` contains FBX models the owner marked Archive in
`AssetReview/decisions.json`. Their Unity `.meta` files are retained alongside
the source files so their GUIDs survive if a choice is reversed. `Previews/`
contains review images for the archived models.

These files live outside Unity's `Assets/` folder so agents cannot drag them
into a scene by accident. Each affected KayKit pack has a copied `LICENSE.txt`
and `SOURCE.md`; the corresponding active pack under `Assets/ThirdParty/KayKit/`
retains its original license, source note, texture atlas, and unarchived models.
