
# Kyber Clash — Text-to-Image Prompts (Playable Forms)

These prompts are designed for a **Star Wars × Super Smash Bros.-style** platform fighter.
Each prompt targets one unique `FormMechanicType` from `FormSO.cs` and is written to generate
**character key art plus a multi-pose sprite sheet** you can cut up in Unity.

## Shared style anchors (append to any prompt as needed)
- Art style: "clean cel-shaded Smash Bros Ultimate key art, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background, no watermark"
- Sprite-sheet spec: "presented as a 6x4 grid sprite sheet: idle, walk, jump, light attack, heavy attack, neutral special, side special, up special, down special, hit, block, KO — each cell evenly spaced on a transparent background, consistent lighting and proportions across all cells"
- Resolution: "high resolution 2048x2048, sharp details, game-ready asset"

---

## 1. CounterStance — Form Shii-Cho (Balanced, generates meter on block)
> Star Wars Jedi in Form Shii-Cho stance, balanced grounded guard with lightsaber held horizontally across the body in a relaxed ready position, feet shoulder-width apart, calm confident neutral expression, blue-white kyber blade with soft glow, earthy tan and brown tunic, utility belt, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, walk, jump, light slash, heavy slash, block-counter stance (blade bracing a glowing energy shield that builds a meter aura), side special, up special, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 2. PrecisionParry — Form Makashi (Dueling, precise parries, ripostes)
> Star Wars Jedi in Form Makashi dueling stance, single-handed elegant fencer grip on a thin elegant lightsaber, blade pointed forward in a precise en garde pose, slender refined build, aristocratic posture, red-cored crimson blade with white core, fitted dark dueling robe with high collar, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, walk, jump, light thrust, heavy lunge, precision-parry spark (blades meeting with bright parry flash then riposte), side special, up special, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 3. PerfectDeflection — Form Soresu (Defensive, reflects projectiles)
> Star Wars Jedi in Form Soresu defensive stance, lightsaber angled diagonally to deflect incoming fire, compact cautious crouch, tight guarded posture, calm focused expression, green kyber blade with steady glow, layered protective robes and pauldrons in muted greens and greys, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, walk, jump, light tap, heavy sweep, perfect-deflection pose (blade angled reflecting a bolt with a curved energy arc), side special, up special, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 4. AcrobaticFlow — Form Ataru (Acrobatic, air mobility, combo extender)
> Star Wars Jedi in Form Ataru mid-air acrobatic flourish, dynamic spinning leap with both hands gripping the lightsaber in a wide spinning arc, energetic expressive face, agile lithe build, yellow-gold kyber blade with motion-trail glow, lightweight flexible tunic in bright golds and whites, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, run, high jump, flip attack, light flurry, aerial combo chain (multiple after-image sabers), side special, up special launching rise, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 5. PowerCounter — Form Shien / Djem So (Power, counter-attacks, heavy hits)
> Star Wars Jedi in Form Shien/Djem So power stance, two-handed heavy grip slamming the lightsaber downward, broad muscular build, grounded aggressive posture, intense determined expression, orange-amber kyber blade with thick glow, heavy reinforced armor plating over tunic in steel and bronze, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, walk, jump, light chop, heavy overhead smash, power-counter absorb (blade bracing then releasing a shockwave), side special, up special, down special ground-pound, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 6. ForceUtility — Form Niman (Balanced, Force powers, utility)
> Star Wars Jedi in Form Niman balanced stance, one hand holding a lightsaber at the side and the other hand extended channeling a glowing Force aura, harmonious neutral expression, medium balanced build, teal-cyan kyber blade with soft glow, balanced robe in teal and beige with sash, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle, walk, jump, light slash, heavy slash, force-push (hand thrust with expanding ring shockwave), force-pull (hand cinching pulling a distant glow), up special, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

## 7. BerserkerTrance — Form Juyo / Vaapad (Aggressive, high risk/reward, meter drain)
> Star Wars Jedi in Form Juyo/Vaapad berserker trance, wild aggressive flurry pose with the lightsaber whipping in erratic arcs, feral intense expression, crackling dark-side energy and ember sparks around the body, violet-to-red corrupted kyber blade with jagged glow, tattered dark robe with glowing veins, clean cel-shaded Smash Bros Ultimate key art style, bold readable silhouette, vibrant flat shading, subtle rim light, transparent background. Sprite sheet: 6x4 grid showing idle trance (eyes glowing, aura flaring), walk, leap, light flurry, heavy berserk slash, meter-drain overload (body enveloped in pulsing power aura), side special, up special, down special, hit react, KO — evenly spaced cells, transparent background, consistent lighting and proportions, 2048x2048 game-ready.

---

## Notes
- `FormMechanicType.None` and `FormMechanicType.Custom` are not distinct playable forms in the
  enum and have no dedicated mechanic, so no unique prompt is generated for them. Use `Custom`
  only when `FormMechanicData` defines a bespoke mechanic, then derive a prompt from the
  seven templates above by swapping the stance/aura descriptors.
- All blades use a `saberColor` / `saberCoreColor` pair in `FormSO`; the colors above match the
  lore of each classic lightsaber form but you can recolor per `FormSO` values.