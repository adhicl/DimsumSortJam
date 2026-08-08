# Next Steps — work to do outside Unity

Everything still required to ship IAP, Google Play login, Cloud Save, and compliant ads. The
Unity-side code and wiring is done; this file is the console/web work that has to happen before
any of it functions on a real device.

**Project facts you'll need:**

| Thing | Value |
|---|---|
| Android package name | `com.yourfavoritegamestudio.Jajanan` |
| Unity org / project | `sinergi-studio` / `DimsumSortJam` |
| Unity cloud project id | `968e9456-24c8-4cf1-941f-e423b05bcf8b` |
| AdMob publisher id (in use) | `ca-app-pub-8590881680208951` |
| Privacy policy URL | https://yourfavoritegamestudio.com/privacy-policy.html |
| Terms of use URL | https://yourfavoritegamestudio.com/terms-of-use.html |

---

## 0. Blockers — nothing works until these are done

- [ ] **Set the Web App Client ID in Unity** (§7). It is currently empty — confirmed: `mWebClientId`
      in `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset` is blank. The GPGS setup window
      labels it "optional", but `RequestServerSideAccess` — the call that turns a Play Games
      session into the auth code UGS needs — throws without it. The game catches that and falls
      back to anonymous, so **Play Games sign-in silently never happens and Cloud Save is keyed
      to a per-install anonymous id instead of the player's Google account.** Everything else in
      §2 is done: App ID `820043441288` and the Android setup are in place.
      `GameServicesController` now logs this as an error on device rather than failing quietly.
- [ ] **Create the 15 in-app products** in Play Console (§1). Until they exist and are Active, the
      shop tab shows `…` on every card and every buy button stays disabled.
- [x] ~~Enable Cloud Save~~ — **verified working** (§3). A live save/load round-trip against the
      real service succeeded from the Editor on 2026-08-08.
- [x] ~~Enable Authentication~~ — **verified working** (§3). Sign-in returns a real UGS player id.
- [x] ~~Replace the AdMob App ID~~ — set to `ca-app-pub-8590881680208951~1081208993`, matching
      the ad units' publisher. Ads are fully configured: real App ID, both banners and the
      rewarded ad on live units.
- [ ] **Publish the two legal pages** at the URLs above (§5). The Settings buttons now open them,
      and Google Play rejects apps whose privacy policy URL 404s.

---

## 1. Google Play Console — in-app products

**Monetize ▸ Products ▸ In-app products** (newer consoles: **One-time products**).

Product IDs must match `Assets/Scripts/Commons/IAPCatalog.cs` character for character — a
mismatch makes the product silently vanish from the shop.

> **No boosters.** Boosters are not implemented in the game, so nothing in the catalog grants
> them. Every bundle that would have carried boosters carries **unlimited-lives time** instead.
> When boosters ship, add a `boosterEach` field back to `IAPCatalog.Reward` and top up the
> bundles — the shop cards will pick the numbers up automatically.

> **Lives are only sold as time.** `Reward.unlimitedLifeHours` buys a window during which lives
> are not consumed, and is what the **bundles** sell — the shop prints it as "2h" / "1d" beside an
> infinity heart, and the Home top bar counts it down. `Reward.lives` still exists for non-purchase
> rewards but no product uses it. See "Lives" below.

### Gold — consumables, repeatable (the 3x2 grid at the bottom of the shop tab)

| # | Product ID | Suggested name | Grants | Suggested price |
|---|---|---|---|---|
| 1 | `coins_500` | Handful of Coins | +500 gold | Rp 15.000 |
| 2 | `coins_1200` | Bag of Coins | +1.200 gold | Rp 29.000 |
| 3 | `coins_3000` | Crate of Coins | +3.000 gold | Rp 65.000 |
| 4 | `coins_8000` | Vault of Coins | +8.000 gold | Rp 149.000 |
| 5 | `coins_20000` | Hoard of Coins | +20.000 gold | Rp 319.000 |
| 6 | `coins_50000` | Fortune of Coins | +50.000 gold | Rp 699.000 |

Gold-per-rupiah improves with every tier (33 → 41 → 46 → 54 → 63 → 72 gold per Rp 1.000), which
is what makes the ladder worth climbing. Keep that shape if you re-price.

### Bundles — consumables, repeatable

**Lives are not sold on their own.** There is no `lives_refill` product: every route out of an
empty life bar goes through a bundle, so the out-of-lives popup and the top bar's heart both
send the player to the shop tab. If you already created `lives_refill` in Play Console,
deactivate it.

| # | Product ID | Name on the card | Gold | Power-ups (each of 4) | Unlimited lives | Suggested price |
|---|---|---|---|---|---|---|
| 7 | `powerup_bundle` | Power-Up Bundle | — | 5 | 1h | Rp 39.000 |
| 8 | `bundle_big` | Big Bundle | 2.500 | 2 | 2h | Rp 82.000 |
| 9 | `bundle_great` | Great Bundle | 5.500 | 3 | 3h | Rp 159.000 |
| 10 | `bundle_ultra` | Ultra Bundle | 12.000 | 6 | 6h | Rp 449.000 |
| 11 | `bundle_superior` | Superior Bundle | 25.000 | 12 | 12h | Rp 790.000 |
| 12 | `bundle_legendary` | Legendary Bundle | 50.000 | 24 | 1d | Rp 1.590.000 |

"Power-ups (each of 4)" means the amount is credited to *every* power-up slot — a Great Bundle
hands over 3 of each, 12 power-ups in total.

### Non-consumables — bought once, restorable

| # | Product ID | Name on the card | Grants | Suggested price |
|---|---|---|---|---|
| 13 | `remove_ads` | No Ads | Hides banner ads permanently | Rp 99.000 |
| 14 | `starter_pack` | Starter Pack | +4.000 gold, 2h unlimited lives, +2 of each power-up | Rp 63.000 |
| 15 | `bundle_no_ads` | No Ads Bundle | Removes ads, +2.000 gold, 1h unlimited lives, +2 of each power-up | Rp 199.000 |

All three turn their card off once owned (`ShopCard`), so the shelf never shows a dead buy
button. `bundle_no_ads` and `starter_pack` are non-consumable specifically so the entitlement
survives a reinstall — `IAPController` re-derives ads-removal from whichever owned product has
`removesAds` set, so a future ads-removing bundle needs no code change.

**Notes**

- [ ] Mark every product **Active**.
- [ ] The app needs at least one build uploaded to a track (Internal testing is fine) before the
      store returns prices. Products on a draft app report "unavailable".
- Prices are a starting point — the game never displays hard-coded prices, it shows the store's
  localized price string. Changing a price in the console needs no code change.
- All 15 products now have UI. Fewer than 15 in the `[IAP]` log line means an ID mismatch or an
  inactive product in the console.

---

## 2. Google Play Console — Play Games login

Three places must agree: Play Console, Google Cloud, and the Unity Dashboard.

### 2a. Play Games Services

**Grow ▸ Play Games Services ▸ Setup and management ▸ Configuration**

- [ ] Create a Play Games Services project and link it to the app.
- [ ] **Credentials ▸ Add credential ▸ type: Game server** → produces an **OAuth Client ID** and
      **Client Secret**. Keep both; they go into Unity's dashboard in §3.
- [ ] **Add credential ▸ type: Android**, linked to your signing certificate SHA-1.
  - Add your **release** SHA-1 and your **debug** SHA-1 as separate credentials, or sign-in will
    only work in one of the two build types.
  - If you use **Play App Signing**, use the SHA-1 shown under **Setup ▸ App signing**, *not*
    your upload key. This is the single most common reason sign-in works in a local build but
    fails from the Play build.
- [ ] Add your Google accounts under **Testers** while the config is unpublished — sign-in fails
      for anyone not on that list.
- [ ] Copy the **Resources Definition** XML (*Configuration ▸ Get resources*). It has to be pasted
      into Unity once — see §7.

Getting the release SHA-1:

```sh
keytool -list -v -keystore publish.keystore -alias <your-alias>
```

### 2b. Unity Cloud Dashboard — identity provider

**Unity Cloud Dashboard ▸ Player Authentication ▸ Identity Providers ▸ Add ▸ Google Play Games**

- [ ] Client ID = the **game server** OAuth Client ID from §2a
- [ ] Client Secret = its Client Secret
- [ ] Enable the provider
- [ ] Leave **Anonymous** sign-in enabled — the game falls back to it when Play Games is
      unavailable or declined, so players who say no can still play.

---

## 3. Unity Cloud Dashboard — services

Checked from the Editor on **2026-08-08** by running the game from `Splash` and watching the real
services answer. What that run proved, and what it could not:

- [x] ~~**Project link**~~ — `cloudProjectId 968e9456-…` and org `sinergi-studio` are set in
      `ProjectSettings.asset`, and the services resolved against them.
- [x] ~~**Authentication** — enable.~~ **Working.** `UnityServices.State = Initialized` and
      sign-in returned a real player id (`[GameServices] Signed in as x2jfgKcQ… (anonymous)`).
      Anonymous is expected in the Editor — Play Games has no Editor implementation, and the
      Web App Client ID is still missing anyway (§0).
- [x] ~~**Cloud Save** — enable.~~ **Working.** `CloudSaveService.Data.Player.SaveAsync` completed
      against the live service, and the boot sync read cleanly
      (`[CloudSave] No cloud record yet; uploading the local save.`). A load that errors logs
      "Download failed" instead, so a clean "no cloud record yet" is proof the service answered.
      The game writes one key, `player_save`, per player.
- [ ] **In-App Purchasing** — enable, and paste the **Google Play license key**
      (Play Console ▸ Monetize ▸ Monetization setup ▸ *Base64-encoded RSA public key*).
      **Not verifiable from the Editor**: `[IAP] 15 products ready` there is the built-in fake
      store answering, not Google Play. This one only proves itself on a real device.
- Economy is still unused; balances live in the Cloud Save snapshot instead.

---

## 4. AdMob console

- [x] ~~App ID~~ — `ca-app-pub-8590881680208951~1081208993`, set in
      `GoogleMobileAdsSettings.asset` and confirmed written into the androidlib manifest.
- [x] ~~Ad units~~ — banner `…/8740207165`, rewarded `…/2518293601`, both live in-scene.
- [x] ~~Banner overlapping the Home tab bar~~ — the Home safe-area panel now reserves the banner's
      height at the bottom (`SafeAreaPanel.padBannerAd`, fed by `BannerAdController`), so the tab
      strip sits above the ad instead of under it.
- [ ] **Confirm in the AdMob console** that this app's status is *Ready* and not
      "Requires attention" — a newly created app can sit in review, and units return no fill
      until it clears.
- [ ] **Privacy & messaging ▸ GDPR** — create and **publish** a consent message for the app.
      Also do **US states** if you serve the US.
      *Without a published message the UMP form never appears, `CanRequestAds()` stays false in
      the EEA, and ads silently stop serving there.*
- [ ] **app-ads.txt** — publish it at `https://yourfavoritegamestudio.com/app-ads.txt` with the
      line AdMob gives you, and set the developer website in your Play listing to that domain.
      Without it you lose most programmatic demand.

---

## 5. Your website

- [ ] Publish **https://yourfavoritegamestudio.com/privacy-policy.html**
- [ ] Publish **https://yourfavoritegamestudio.com/terms-of-use.html**
- [ ] Publish **https://yourfavoritegamestudio.com/app-ads.txt** (see §4)

The privacy policy must disclose that the game uses **Google AdMob** (advertising ID, ad
personalization), **Google Play Games** (account identifier), **Unity Gaming Services**
(pseudonymous player id), and **Google Play Billing** (purchase records). Play's Data safety
review checks that the policy actually covers what the app does.

---

## 6. Play Console — store listing & policy

- [ ] **Policy ▸ App content ▸ Privacy policy** → the privacy-policy URL above.
- [ ] **Data safety** form — declare data collected by ads and IAP. This is a review blocker if
      it disagrees with your privacy policy.
- [ ] **Ads** declaration → *Yes, this app contains ads*.
- [ ] **App content ▸ Target audience** — if the audience includes children, the ad and consent
      requirements change materially (and `tagForUnderAgeOfConsent` on the `Consent` object in
      Splash should be turned on).

### Android permissions the build requests

Declared in `Assets/Plugins/Android/AppPermissions.androidlib/AndroidManifest.xml`. Unity merges
that with the plugin manifests; the Data safety form has to match this list.

| Permission | Why | Data safety |
|---|---|---|
| `INTERNET` | UGS auth, Cloud Save, analytics, ads, billing | — |
| `ACCESS_NETWORK_STATE` | Connectivity check before network calls | — |
| `com.google.android.gms.permission.AD_ID` | AdMob advertising ID (required from targetSdk 33) | Declare: **Device or other IDs**, used for **Advertising** |
| `com.android.vending.BILLING` | Google Play in-app purchases | Declare: **Purchase history** |

Cloud Save also means you now store player data server-side — the Data safety form must declare
**App activity ▸ In-app search history / other actions** (game progress) and the pseudonymous
player id, linked to the user and stored on Unity's servers. The privacy policy needs the same.

No location, storage, camera, contacts, or phone-state permission is requested, and none should
appear in the merged manifest. If one shows up after adding an SDK, that is a new Data safety
disclosure — check `Temp/gradleOut/launcher/build/intermediates/merged_manifests/` after a build.

---

## 7. One-off steps back inside Unity

Small tasks that pair with the console work above, listed so nothing is missed:

- [x] ~~**Window ▸ Google Play Games ▸ Setup ▸ Android setup** → Resources Definition XML~~ — done.
      App ID `820043441288` and package `com.yourfavoritegamestudio.Jajanan` are written into
      `PlayGamesSettings.asset` and the androidlib manifest.
- [ ] **Same window: fill in "Web App Client ID"** — the one field still empty, and a launch
      blocker (see §0). Use the **game server** OAuth client ID from §2a (the long
      `…apps.googleusercontent.com` string, *not* the Android credential). Paste it, click
      **Setup**, and confirm `mWebClientId` is non-empty in
      `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset`.
- [x] ~~AdMob App ID~~ — done. For future reference: set it only in
      `GoogleMobileAdsSettings.asset`; the plugin's `ManifestProcessor` rewrites
      `GoogleMobileAdsPlugin.androidlib/AndroidManifest.xml` on every Android build, so
      hand-edits to that manifest are overwritten.
- [x] ~~Home/Game banner `useTestAd` mismatch~~ — both are now **unticked (live)** and share
      banner unit `ca-app-pub-8590881680208951/8740207165`. The rewarded ad in Game is also live
      (`…/2518293601`).

> [!WARNING]
> Live ad units are now active in the Editor and in every build. **Never click your own ads** —
> that is invalid traffic and can get the AdMob account suspended. While testing, either add
> your device as an AdMob test device, or temporarily re-tick `useTestAd`.
- [ ] Optionally set the **Google Play license key** and run
      **Services ▸ In-App Purchasing ▸ Receipt Validation Obfuscator** to enable local receipt
      validation (see Known gaps). No `Tangle` file exists yet, so this has not been done.

---

## 8. Testing

- [ ] Upload a build to **Internal testing**. Prices only appear for an app with a release on a
      track, matching package name and version code.
- [ ] **Setup ▸ License testing** → add tester Google accounts. They see real purchase dialogs
      marked *test* and are never charged.
- [ ] Testers must be on **both** the internal-testing list and the Play Games testers list.
- [ ] To see the consent form outside the EEA: on the `Consent` object in Splash set
      **Debug Geography** to `EEA` and add your device's hashed id to **Test Device Hashed Ids**
      (the Ads SDK prints it in logcat on first run). Set it back to `Disabled` for release.

**What a good first device run looks like in logcat:**

```
[GameServices] Signed in as <id> (Google Play Games).
[CloudSave] No cloud record yet; uploading the local save.
[Consent] Status=Obtained, canRequestAds=True
[IAP] 15 products ready.
```

Fewer than 15 products means an ID mismatch or an inactive product in the console.
`(anonymous)` instead of `(Google Play Games)` means §0's Web App Client ID is still missing.

**Testing Cloud Save end to end:** play past a level or two, wait ~5 seconds for the upload
(`[CloudSave]` logs it), uninstall, reinstall, and sign in with the same Google account. The
loading bar holds at 90% while it pulls, then you should see
`[CloudSave] Restored cloud save (level N, X gold)`. This only works once sign-in is real —
with anonymous auth the reinstall gets a brand new player id and finds nothing.

**In the Editor:** IAP runs against a built-in fake store — clicking Buy pops a confirm dialog
and approving credits the reward for real, so the shop can be exercised without any console
setup. Play Games and UMP have no Editor implementation, so sign-in is always anonymous and the
consent form never appears there. **Play from `Splash`** — starting in Home skips the controllers
and leaves buy buttons disabled.

---

## Lives

Two states, one heart in the top bar.

**Normal.** `GameSetting.totalLife` counts down from `GameSetting.MaxLife` (**5**). Losing a level
costs one — `GameController.ShowLose()` calls `TrySpendLife()`, which is the only place in the
game a life is spent. Every give-up path funnels through `ShowLose`, so there is one hook, not
several. The top bar shows a plain heart and the number.

**Unlimited.** `unlimitedLivesUntil` is a Unix-ms deadline; while it is in the future
`TrySpendLife` is a no-op and nothing is charged, for exactly that much real time. The top bar
shows the ∞ glyph over the heart and counts down.

**When a window runs out, lives go back to a full 5**, and the regeneration clock is cleared.

**Lives regenerate: one every 30 minutes** (`LifeRegenMinutes`), up to the cap. `nextLifeAt` is a
Unix-ms deadline like the window, so lives keep coming back while the app is closed — reopening
after three hours at zero returns a full bar, not one life.

Both clocks are driven by a single entry point, **`RefreshLives()`**, so no caller has to remember
there are two. It is idempotent and cheap, and runs from four places without coordination: boot,
the Home top-bar tick, the Play button, and every spend. The Home tick is what makes the heart
update while the player is sitting on the screen rather than at the next launch.

Two rules that are easy to get wrong:

- **The clock starts on the way down from full, and only then.** A second loss must not push the
  pending life further away, or losing twice would cost more than twice.
- **Regeneration never back-pays.** A save written before the feature existed (short bar, no
  clock) starts a fresh 30 minutes rather than crediting for the gap.

### Where the time comes from

| Source | Amount |
|---|---|
| Free, once a calendar day | 15 minutes (`FreeUnlimitedMinutesPerDay`) |
| Bundles | 1h – 1d, see §1 |

(Regeneration returns individual lives, not unlimited time — the two are separate.)

The daily window is granted in `LoadingScene`, and deliberately **after** the Cloud Save pull has
resolved — running it earlier would hand a second window to someone who already claimed today's on
another device. `lastFreeUnlimitedDay` is a local `yyyy-MM-dd` string, so "once a day" means a
calendar day the player recognises rather than a rolling 24 hours.

### Rules worth knowing

- **Windows stack, they don't replace.** New time is added to whichever is later, `now` or the
  existing deadline — so a second purchase never shortens the first, and never back-credits time
  that already lapsed.
- **A cloud sync takes the later deadline**, not the downloaded one, so paid time cannot be lost
  to a device swap. `lastFreeUnlimitedDay` takes the later date for the same reason. `nextLifeAt`
  is the exception: it travels with the life count it belongs to and is applied wholesale, since
  keeping this device's timer against downloaded lives would either hand out a free life or
  restart a wait the player already sat through.
- **The deadline is wall-clock**, so a window keeps burning while the app is closed.
- **`totalLife` is clamped to `MaxLife` on load.** Saves written before the cap existed hold
  values like 999; without the clamp the life economy would be a no-op for every existing tester.

### Display format

`Settings.GetCountdownFormat` always shows two segments so it fits the top-bar pill:

| Remaining | Shows |
|---|---|
| 8 hours | `8:00` (hours:minutes) |
| 7h 59m | `7:59` |
| 15 minutes | `15:00` (minutes:seconds) |
| 59 seconds | `0:59` |

### The out-of-lives gate

`PlayButton.GoToNextScene()` is the only place a level is started from Home, so it is the only
place that gates. Before deciding it brings the life state up to date — settle a window that
lapsed while the app was closed, then try the daily grant, which covers a player who crossed
midnight without ever passing through the loading screen. Only if `CanStartLevel` is still false
does `Out-Of-Lives-Popup` open instead of the level loading.

The popup states both ways back in — wait for the next life, or buy a bundle — and prints the
real wait ("Next life in 27m"). Its button calls `ShopTab.Show()`, which slides Home's pager to
the shop; the top bar's heart does the same, since the lives-only popup is retired.

If the popup prefab is ever unassigned the gate **fails open** — a missing reference logs a
warning and lets the player through rather than locking them out of the game.

Winning is never gated: `WinPopup` chains straight into the next level, and lives are only spent
on a loss, so a player on their last life can keep going as long as they keep winning.

### Still to do

- **No rewarded-ad route out.** `RewardedAdController` already exists for the win-screen double
  and the in-level revive; offering "watch an ad for a life" on the gate would be a small
  addition and softens the 30-minute wait.
- **The top bar shows the life count, not the regeneration timer.** There is one label on the
  heart and the ∞ countdown already owns it. If you want "3 · 12:41", the label needs splitting.
- **`lifeTimer` is now dead weight.** It predates all of this, is saved and synced, and only
  `WinPopup` reads it. Removing it means a save-format change, so it was left alone.

---

## The shop tab

The shop is a tab in the Home scene (`Home.unity` → `Page 1` → `Panel-Shop`), not a popup. It is
a single vertical scroll laid out to match the reference screenshots in `screenshot shops/`:

| Section | Contents |
|---|---|
| **SPECIAL PACK** | `starter_pack`, with a "BEST VALUE" flash |
| **NO ADS** | `remove_ads` |
| **BUNDLES** | the 7 bundles from §1, cheapest first |
| **GOLD** | the 6 gold tiers as a 3x2 grid |

**It is generated, not hand-built.** `Assets/Editor/ShopTabBuilder.cs` runs in two stages:

1. **Tools ▸ Shop ▸ Rebuild Shop Prefabs Only** writes the row templates to
   `Assets/Prefabs/UI/Generated/Shop/`:

   | Prefab | What it is |
   |---|---|
   | `Shop-Section-Header.prefab` | The full-width section bar |
   | `Shop-Bundle-Row.prefab` | One bundle row, every optional piece included |
   | `Shop-Gold-Tile.prefab` | One gold tile |
   | `Shop-Gold-Panel.prefab` | The 3-column grid the tiles sit in |

   Alongside them, `Illustrations/` holds the **hand-made** animated art the No Ads cards use —
   `Shop-Illustration-Sealbear.prefab` and `Shop-Illustration-Bunny-Mail.prefab`, lifted from the
   kit's other panels. The builder only *places* these; it never generates or overwrites them,
   and "Rebuild Shop Prefabs Only" leaves them alone. A card opts in with `IllustrationPath` on
   its `CardSpec`, which also hides that card's flat icon. Placement lives in the illustration
   prefab itself, so moving the art moves with it.

2. **Tools ▸ Shop ▸ Rebuild Shop Tab** does that, then instantiates those prefabs into Page 1 and
   fills them in. The rows in the scene are real prefab instances, so restyling every bundle at
   once means editing `Shop-Bundle-Row.prefab` — no code, no rebuild.

Every amount printed on a card is read from `IAPCatalog.Rewards`, so the shelf can never
advertise something different from what the purchase grants. Prices are never generated —
`IAPBuyButton` fills them in from the store at runtime.

### Built out of CuteKawaiiGUIPack, not invented

The rows are assembled from kit parts so the shop reads as part of the game rather than as
something bolted on. Same construction as the kit's own rows (`Messages-Item`, `Shop-Item`):

- Every card is the kit's five-layer `Rectangle-Outline-Shadow` frame — drop shadow, outline,
  shading, fill, curved highlight. That stack is most of why the kit's screens look coherent.
- Every reward well is a `Rectangle-Outline`; the badge is the kit's `Sign-Sale` flash; section
  bars are `Headline-<Colour>` with the hug-the-text fitter removed.
- **Colours are read out of the kit's own prefabs at build time**, not hard-coded. A card names a
  colour family (`Orange`, `Berry`, `Violet`…) and the builder copies that family's frame, well
  and text colours off `Rectangle-Outline-Shadow-<Family>` and `Headline-<Family>`. A card can
  only ever be a colour CuteKawaiiGUIPack already ships, and re-tinting the kit re-tints the shop.

### Responsiveness

The CanvasScaler references 1080x1920 and matches width/height evenly, so the canvas is **not**
1080 units wide on most phones — a 20:9 screen reports about 966, a 4:3 tablet about 1247. Rows
stretch to the content width so they were fine, but the gold grid was authored at exactly
3 x 320 + 2 x 30 = 1020 and **overflowed the screen on every modern tall Android phone**.

`Assets/Scripts/UI/ResponsiveGrid.cs` on `Shop-Gold-Panel` fixes that: it divides whatever width
the grid really has between the columns and keeps the cells' shape. Measured across six devices,
the grid now fits from 966 to 1247 units of canvas width (cells range 281 → 375 wide).

The subtle part, if you ever touch it: a `GridLayoutGroup` advertises a minimum width of
`columns x cellSize`, and a parent layout honours that — so the grid pushes itself wider than the
screen and the cell size never gets a chance to come down. `ResponsiveGrid` therefore also forces
its `LayoutElement` to `minWidth = 0, preferredWidth = 0, flexibleWidth = 1`, declaring that its
width comes from the parent. Without that the fitter looks correct and changes nothing.

Consequences worth knowing:

- **Hand edits inside the scene's `Shop-Generated` are lost on the next rebuild** — but edits to
  the four prefabs are not. Layout and styling belong in the prefabs; which rows exist, and their
  colour families, belong in `ShopTabBuilder.cs`.
  **Rebuild Shop Tab lists anything added by hand and asks before destroying it** rather than
  silently wiping it. If you drop art straight into a card in the scene it is still throwaway —
  save it under `Illustrations/` and point a `CardSpec` at it, the way the two No Ads
  illustrations are handled, and it survives every rebuild.
- The ∞ on the unlimited-lives hearts is not in Dosis, so TMP falls back to LiberationSans and
  bakes the glyph on demand. That fallback is a **Dynamic** atlas with its source TTF referenced,
  so the glyph is produced at runtime — the committed atlas file is only a cache, and a diff
  appearing in it after a play session is noise, not a required asset change.
- **Rebuild Shop Tab no longer regenerates the prefabs** (it only does so if they are missing).
  The prefabs are meant to be edited once generated, and rewriting them on every tab rebuild
  would throw that away. Use **Rebuild Shop Prefabs Only** to go back to the generated styling —
  that one *does* overwrite.
- Adding a product is two edits: `IAPCatalog.cs` (id, type, reward) and the matching
  `CardSpec`/tier array in `ShopTabBuilder.cs`. Then rebuild, then create it in Play Console.
- Filling a row in never adds or removes objects — every optional piece (art, power-up grid,
  extras, description, badge) exists in the prefab and is switched off per card, so instances
  carry value overrides only and stay easy to read in the Inspector.
- **Nothing on the page may use the Green family**: the page's own tiles are Green, so a Green
  bar or card disappears into them. That is why the GOLD header is Yellow.
- Rebuilding while Home is open logs a `TMP_SubMeshUI.UpdateMaterial` NullReferenceException.
  It is Unity's global graphic-rebuild pass poking stale TMP sub-meshes in the deactivated
  `(unused template)` subtree, not the shop — rebuilding with an empty scene open is silent, and
  deleting the retired template stops it for good.
- The old UI-kit sample shop is still in the scene as `Header (unused template)` and
  `Content (unused template)`, both switched off. Delete them once you are happy with the
  generated shop.
- The shop list is a `NestedScrollRect`, not a plain `ScrollRect`. It lives inside the Home
  scene's horizontal pager, and a plain ScrollRect eats the horizontal swipe — the player would
  be stuck on the shop page with only the tab buttons to get out.
- The coin sprites in `CuteKawaiiGUIPack` are numbered **biggest hoard first**
  (`Coins-1-Chest` … `Coins-6` is a single coin), which is why `ShopTabBuilder` aliases them to
  `Coin1`..`Coin6` in ascending order. Use the aliases.
- The background is the same scrolling tile prefab the other tabs use, in the shop's own colour:
  Home is `Backgrund-Tiles-Blue`, Missions `Backgrund-Tiles-Violet`, shop `Backgrund-Tiles-Green`.
- **Everything that used to open a currency popup now opens this tab.** Both top-bar pills carry
  `ShopTabButton`; `Coins-Shop-Popup` and `Lives-Shop-Popup` are unreferenced and can be deleted.
  Only the in-level popups (`Out-Of-Move-Popup`) still use `PopupOpener` for currency, because
  the shop tab does not exist in the Game scene — `ShopTab.Show()` logs a warning and no-ops
  wherever there is no PagedRect.

---

## Reference — what is already wired in Unity

| File / object | Role |
|---|---|
| `Assets/Scripts/Commons/IAPCatalog.cs` | The 15 products, types, and rewards. Single source of truth. |
| `Assets/Editor/ShopTabBuilder.cs` | Generates the shop row prefabs and the Home shop tab from the catalog. |
| `Assets/Prefabs/UI/Generated/Shop/` | The four generated row templates — edit these to restyle. |
| `Assets/Scripts/Controllers/IAPController.cs` | Store connection, two-step purchase flow, granting, restore, dedup ledger, non-consumable ownership. |
| `Assets/Scripts/Controllers/GameServicesController.cs` | UGS init, Play Games sign-in with anonymous fallback, `LinkWithPlayGamesAsync`. |
| `Assets/Scripts/Controllers/CloudSaveController.cs` | Cloud Save sync: pull on boot, conflict resolution, coalesced upload after every save. |
| `Assets/Scripts/Controllers/BannerAdController.cs` | Bottom banner, and publishes its height so UI can inset around it. |
| `Assets/Plugins/Android/AppPermissions.androidlib/` | The app's Android permissions, declared explicitly in one file. |
| `Assets/Scripts/Controllers/ConsentController.cs` | UMP privacy flow; `WhenAdsAllowed()` gates ad SDK startup. |
| `Assets/Scripts/UI/IAPBuyButton.cs` | Buy button: localized price, availability, purchase. |
| `Assets/Scripts/UI/ShopCard.cs` | Hides a one-time product's card once it is owned. |
| `Assets/Scripts/UI/ShopTab.cs` | `ShopTab.Show()` — slides Home's pager to the shop from anywhere. |
| `Assets/Scripts/UI/ShopTabButton.cs` | Sends a button to the shop tab; on the top bar's heart. |
| `Assets/Scripts/UI/OutOfLivesPopup.cs` | The gate's copy and its "get lives" route to the shop. |
| `Assets/Scripts/UI/PlayButton.cs` | Starts a level, or opens the gate when there are no lives. |
| `Assets/Scripts/UI/NestedScrollRect.cs` | Vertical scrolling inside the horizontal pager without stealing swipes. |
| `Assets/Scripts/UI/ResponsiveGrid.cs` | Divides a GridLayoutGroup's cells out of its real width, for the gold grid. |
| `Assets/Scripts/UI/SafeAreaPanel.cs` | Notch/gesture-bar insets, plus the banner-ad inset on Home. |
| `Assets/Scripts/UI/HomeScene.cs` | Home top bar: gold, and the heart's count / ∞ countdown. |
| `Assets/Scripts/Commons/GameSetting.cs` | Save data, plus the life economy: cap, spend, settle, daily grant. |
| `Assets/Scripts/UI/RestorePurchasesButton.cs` | Wires the previously dead "Restore Purchases" button. |
| `Assets/Scripts/UI/PrivacyOptionsButton.cs` | "Ad Privacy" entry; self-hides where not legally required. |
| `Assets/Scripts/UI/OpenUrlButton.cs` | Opens the privacy/terms URLs. |
| `Splash.unity` | `GameServices`, `Consent`, `IAP` singletons (`DontDestroyOnLoad`). Boot scene, build index 0. |
| `Home.unity` | Shop tab (Page 1); both top-bar pills (`Credits-Life`, `Credits-Coins`) → shop tab. |
| `Assets/Prefabs/UI Popups/Coins-Shop-Popup.prefab` | **Unreferenced.** The old 4-tier coin popup; the shop tab replaced it. |
| `Assets/Prefabs/UI Popups/Lives-Shop-Popup.prefab` | **Unreferenced.** The old lives popup; lives are only sold in bundles now. |
| `Assets/Prefabs/UI Popups/Out-Of-Lives-Popup.prefab` | The out-of-lives gate; its button opens the shop tab. |
| `Settings-Popup Food Sort Home.prefab` | Restore Purchases, Ad Privacy, Terms, Privacy Policy. |

**Purchase safety.** Rewards are granted and saved *before* the order is confirmed with the
store, so a crash mid-purchase re-delivers rather than losing it. A transaction-id ledger in
PlayerPrefs stops a re-delivered order paying out twice. Non-consumable ownership is re-derived
from the store on every launch, so `remove_ads` survives a reinstall.

---

## Known gaps

- **Receipt validation is not enforced.** Purchases are granted on trust; there is a `TODO` at
  the grant site in `IAPController.cs`. Fine for launch, worth closing before the game earns
  enough to be worth attacking.
- **Cloud Save trusts the client.** The snapshot is whatever the device uploads, so a modified
  build can grant itself gold. Same trade-off as the unvalidated receipts above, and the same
  fix if it ever matters: move balances into Economy, which is server-authoritative.
- **Cloud saves are only as good as the account.** Until the Web App Client ID is set (§0),
  players are anonymous, and an anonymous id does not survive an uninstall — so Cloud Save
  restores nothing on a reinstall. The service itself is confirmed working; the identity is not.
- **Boosters are unimplemented and unsold.** `GameSetting` still saves `totalBooster1..3` so old
  save data survives, but nothing grants or spends them.
- **Every life clock trusts the device clock.** Winding forward regenerates lives and ends an
  unlimited window early; winding back extends the window, and rolling the date claims another
  free 15 minutes. Same trust model as the save data generally — the fix, if it ever matters, is
  a server timestamp.
- **No timed / limited offers.** The reference shop's "Limited Pack" has a countdown; the
  SPECIAL PACK section here is a plain one-time `starter_pack` with no timer.
- **"Rate Us" is still unwired.** Add `OpenUrlButton` with
  `market://details?id=com.yourfavoritegamestudio.Jajanan` once the app is live.
- **The in-game Settings popup** (`Settings-Popup Food Sort.prefab`, shown during a level) has no
  Restore / Ad Privacy / legal links — only the Home one does.
- **`Splash` has a hidden `Button-Login`** opening a demo email/password popup. It was already
  inactive and does nothing; sign-in is automatic. Delete it, or repoint it at
  `GameServicesController.LinkWithPlayGamesAsync()` to let anonymous players upgrade.
