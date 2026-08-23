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

- [x] ~~**Set the Web App Client ID in Unity**~~ — **done, verified in the project on 2026-08-17.**
      `mWebClientId` in `Assets/GooglePlayGames/Resources/PlayGamesSettings.asset` is
      `820043441288-gkbt1qknq3bt9mog17uaacn6nqeh6qgg.apps.googleusercontent.com`, matching
      `and.ClientId` in `ProjectSettings/GooglePlayGameSettings.txt`, with
      `android.SetupDone=true`. The numeric prefix matches App ID `820043441288`, so it belongs to
      the right Play Games project.
      **Still to confirm on device:** that this is the **game server (web)** OAuth client and not
      the Android one — both end in `.apps.googleusercontent.com` and the two cannot be told apart
      from the string alone. Proof is a logcat line reading `(Google Play Games)` rather than
      `(anonymous)` (§8). Until that run happens, Cloud Save identity is still unproven.
- [ ] **Create the 15 in-app products** in Play Console (§1) — **reported done 2026-08-17, not yet
      verified.** Play Console state lives outside this repo, so nothing here can confirm it: the
      Editor's `[IAP] 15 products ready` is Unity's built-in *fake* store answering, not Google
      Play. The only proof is that same line from a **device build signed and installed from a
      Play track**, with a licence-tester account. Fewer than 15 means an ID mismatch or a product
      left Inactive — cross-check against the ID list in §1, which is generated from
      `IAPCatalog.cs` and must match character for character.
- [x] ~~Enable Cloud Save~~ — **verified working** (§3). A live save/load round-trip against the
      real service succeeded from the Editor on 2026-08-08.
- [x] ~~Enable Authentication~~ — **verified working** (§3). Sign-in returns a real UGS player id.
- [x] ~~Replace the AdMob App ID~~ — set to `ca-app-pub-8590881680208951~1081208993`, matching
      the ad units' publisher. Ads are fully configured: real App ID, both banners and the
      rewarded ad on live units.
- [x] ~~**Publish the two legal pages**~~ — **both live, fetched 2026-08-17** (§5). `app-ads.txt`
      is up too, carrying the correct publisher line for `pub-8590881680208951`.
- [ ] **Extend the privacy policy** (§5). The published page covers Google AdMob / advertising ID
      and Google Play Games, but **does not mention Unity Gaming Services (pseudonymous player id,
      Cloud Save) or Google Play Billing (purchase records)** — both of which the app uses. Play's
      Data safety review compares the form against the policy, and this gap is exactly the kind of
      mismatch that gets a submission rejected.

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

> **Two currencies below.** **USD** is the price to enter if the Play developer account's home
> currency is US dollars; **IDR** if it is Indonesian rupiah. You only ever type *one* of them —
> Play auto-converts every other country from whichever you enter, and you can override
> individual countries afterwards. Every USD figure is a standard Play price point, and the IDR
> column is the same ladder at **~Rp 16.000 / $1**, rounded to a rupiah charm price.
>
> **The Grants column is written to be pasted into the Play Console product description**, so it
> reads as a sentence rather than as table shorthand. If your store listing is in English, switch
> the thousands separator from `.` to `,` on the way in (`2.500` → `2,500`); leave it as-is for an
> Indonesian listing.

### Gold — consumables, repeatable (the 3x2 grid at the bottom of the shop tab)

| # | Product ID | Suggested name | Grants | Price (USD) | Price (IDR) |
|---|---|---|---|---|---|
| 1 | `coins_500` | Handful of Coins | 500 gold | $0.99 | Rp 15.000 |
| 2 | `coins_1200` | Bag of Coins | 1.200 gold | $1.99 | Rp 29.000 |
| 3 | `coins_3000` | Crate of Coins | 3.000 gold | $3.99 | Rp 65.000 |
| 4 | `coins_8000` | Vault of Coins | 8.000 gold | $9.99 | Rp 159.000 |
| 5 | `coins_20000` | Hoard of Coins | 20.000 gold | $19.99 | Rp 319.000 |
| 6 | `coins_50000` | Fortune of Coins | 50.000 gold | $39.99 | Rp 639.000 |

Value improves with every tier, which is the only reason to climb the ladder — **505 → 603 →
752 → 801 → 1.001 → 1.250** gold per $1, or 33 → 41 → 46 → 50 → 63 → 78 gold per Rp 1.000. Keep
that shape if you re-price: a tier that is worse value than the one below it will never sell.

### Bundles — consumables, repeatable

**Lives are not sold on their own.** There is no `lives_refill` product: every route out of an
empty life bar goes through a bundle, so the out-of-lives popup and the top bar's heart both
send the player to the shop tab. If you already created `lives_refill` in Play Console,
deactivate it.

| # | Product ID | Name on the card | Grants | Price (USD) | Price (IDR) |
|---|---|---|---|---|---|
| 7 | `powerup_bundle` | Power-Up Bundle | 5 of each power-up (20 total) and 1 hour of unlimited lives | $2.99 | Rp 47.000 |
| 8 | `bundle_big` | Big Bundle | 2.500 gold, 2 of each power-up (8 total) and 2 hours of unlimited lives | $4.99 | Rp 79.000 |
| 9 | `bundle_great` | Great Bundle | 5.500 gold, 3 of each power-up (12 total) and 3 hours of unlimited lives | $9.99 | Rp 159.000 |
| 10 | `bundle_ultra` | Ultra Bundle | 12.000 gold, 6 of each power-up (24 total) and 6 hours of unlimited lives | $29.99 | Rp 479.000 |
| 11 | `bundle_superior` | Superior Bundle | 25.000 gold, 12 of each power-up (48 total) and 12 hours of unlimited lives | $49.99 | Rp 799.000 |
| 12 | `bundle_legendary` | Legendary Bundle | 50.000 gold, 24 of each power-up (96 total) and a full day of unlimited lives | $99.99 | Rp 1.599.000 |

**"N of each power-up" is not a typo.** There are four power-up slots and a bundle credits the
same amount to *every* one of them, so a Great Bundle hands over 3 of each — 12 power-ups in
total. Both numbers are in the Grants text because the per-slot figure is what the shop card
prints, and the total is what makes the bundle sound worth its price on the store page.

**Unlimited lives** is a window of real time during which losing a level costs nothing — it is
not a stack of lives. Worth saying plainly in the store description, because "6 hours of
unlimited lives" is otherwise easy to misread as an amount rather than a duration.

### Non-consumables — bought once, restorable

| # | Product ID | Name on the card | Grants | Price (USD) | Price (IDR) |
|---|---|---|---|---|---|
| 13 | `remove_ads` | No Ads | Removes banner ads permanently | $5.99 | Rp 99.000 |
| 14 | `starter_pack` | Starter Pack | 4.000 gold, 2 of each power-up (8 total) and 2 hours of unlimited lives | $3.99 | Rp 65.000 |
| 15 | `bundle_no_ads` | No Ads Bundle | Removes banner ads permanently, plus 2.000 gold, 2 of each power-up (8 total) and 1 hour of unlimited lives | $12.99 | Rp 199.000 |

All three turn their card off once owned (`ShopCard`), so the shelf never shows a dead buy
button. `bundle_no_ads` and `starter_pack` are non-consumable specifically so the entitlement
survives a reinstall — `IAPController` re-derives ads-removal from whichever owned product has
`removesAds` set, so a future ads-removing bundle needs no code change.

**Notes**

- [ ] Mark every product **Active**.
- [ ] The app needs at least one build uploaded to a track (Internal testing is fine) before the
      store returns prices. Products on a draft app report "unavailable".
- Prices are a starting point — the game never displays hard-coded prices, it shows the store's
  localized price string. Changing a price in the console needs no code change, and no currency
  in the tables above is ever compiled into the build.
- After Play auto-converts, **spot-check the ladder in a couple of big markets** (US, ID, IN, BR).
  Conversion rounds per country and can occasionally flatten two neighbouring tiers into the same
  price, which kills the reason to buy the bigger one.
- **The Grants column is derived from `IAPCatalog.Rewards`, not from the console.** Play Console
  descriptions are free text and nothing validates them against the build, so a store page can
  promise a number the game does not grant. If you change a reward in `IAPCatalog.cs`, the shop
  card updates itself but the store description does not — edit it here and in Play Console.
- Two prices repeat across sections on purpose: `coins_3000` / `starter_pack` at $3.99, and
  `coins_8000` / `bundle_great` at $9.99. In both pairs the non-gold product is the better deal
  at the same price, which is the point — it is what makes the Starter Pack and the bundles read
  as bargains next to plain gold.
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
- [ ] **Privacy & messaging ▸ GDPR** — create and **publish** a consent message (§4a below).
      *Without a published message the UMP form never appears, `CanRequestAds()` stays false in
      the EEA, and ads silently stop serving there — with no error to explain why.*
- [x] ~~**app-ads.txt**~~ — live at `https://yourfavoritegamestudio.com/app-ads.txt`, fetched
      2026-08-17, carrying `google.com, pub-8590881680208951, DIRECT, f08c47fec0942fa0` — the
      publisher actually serving the ads. Still set the developer website in your Play listing to
      that domain, or the file is never looked for.

### 4a. GDPR consent message

> **This is not something to build in the game.** The form's UI is authored in the AdMob console,
> downloaded at runtime, and drawn by Google's SDK. A hand-built Unity popup would not be a
> Google-certified CMP, so the consent it collected would not be recognised. The Unity half is
> already done and needs no changes — see the end of this section.

**AdMob ▸ Privacy & messaging ▸ European regulations (GDPR) ▸ Create message**

1. **Pick this app.** Messages are per-app, not per-account — one published against a different
   app in the same account does nothing here.
2. **Consent options.** Include *Consent* and *Manage options*. *Do not consent* is optional; leave
   it off and the only route to refusing is through *Manage options*.
3. **Ad partners.** The default Google list is fine. The partner list is part of what the player
   consents to, so widening it later re-prompts everyone who already answered.
4. **Privacy policy URL** → `https://yourfavoritegamestudio.com/privacy-policy.html` (live, checked
   2026-08-17). The message will not publish against a URL that does not resolve.
5. **Publish it.** *Save* alone leaves the message inactive, and an inactive message behaves
   exactly like no message at all — this is the single most common reason the form never appears.
   Confirm the status reads **Published**, not *Draft*.
6. **US states.** If you serve the US, repeat under **US state regulations** — it is a separate
   message and is not covered by the GDPR one.

**This is blocked by the item above it:** while the app sits in *Requires attention*, no unit
returns fill and the consent flow cannot be exercised end to end.

**Proving it works.** On a device in a regulated region, logcat prints (from
`ConsentController.cs:88`):

```
[Consent] Status=Obtained, canRequestAds=True
```

Outside the EEA the form correctly never appears. To see it anyway, on the `Consent` object in
`Splash.unity` set **Debug Geography** to `EEA` and paste the device's hashed id into **Test Device
Hashed Ids** — the Ads SDK prints that id in logcat on the first run. **Set it back to `Disabled`
before shipping.**

**No Unity work is required for any of this.** `Assets/Scripts/Controllers/ConsentController.cs`
already runs the flow on boot from `Splash` and gates every ad-SDK start behind `WhenAdsAllowed`,
and `Assets/Scripts/UI/PrivacyOptionsButton.cs` (on `Settings-Popup Food Sort Home.prefab`) already
provides the "change your choice" entry GDPR requires, hiding itself where it is not required.

---

## 5. Your website

All three fetched and confirmed live on **2026-08-17**:

- [x] ~~Publish **https://yourfavoritegamestudio.com/privacy-policy.html**~~ — live, dated 1 Aug 2026.
- [x] ~~Publish **https://yourfavoritegamestudio.com/terms-of-use.html**~~ — live.
- [x] ~~Publish **https://yourfavoritegamestudio.com/app-ads.txt**~~ — live, and the line matches
      the publisher actually serving the ads:
      `google.com, pub-8590881680208951, DIRECT, f08c47fec0942fa0`.

The privacy policy must disclose that the game uses **Google AdMob** (advertising ID, ad
personalization), **Google Play Games** (account identifier), **Unity Gaming Services**
(pseudonymous player id), and **Google Play Billing** (purchase records). Play's Data safety
review checks that the policy actually covers what the app does.

- [ ] **Two of those four are missing from the published page.** It covers AdMob and Play Games;
      it says nothing about **Unity Gaming Services** or **Google Play Billing**. Add a line for
      each — UGS stores a pseudonymous player id plus game progress on Unity's servers, and Play
      Billing means purchase records. This has to agree with the Data safety form in §6.

---

## 6. Play Console — store listing & policy

- [ ] **Policy ▸ App content ▸ Privacy policy** → the privacy-policy URL above.
- [ ] **Data safety** form — declare data collected by ads and IAP. This is a review blocker if
      it disagrees with your privacy policy.
- [ ] **Ads** declaration → *Yes, this app contains ads*.
- [ ] **App content ▸ Target audience** → declare a **general audience** (not children).
      Decided 2026-08-17. That matches the build: `tagForUnderAgeOfConsent` is `false` on the
      `Consent` object in `Splash.unity`, so the standard GDPR consent flow applies and
      personalized ads stay available.
      If that ever changes, three things move together — turn `tagForUnderAgeOfConsent` on
      (which disables personalized ads), redo this declaration, and revisit the Data safety form.

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
- [x] ~~**Same window: fill in "Web App Client ID"**~~ — done, `mWebClientId` is set to
      `820043441288-gkbt1qknq3bt9mog17uaacn6nqeh6qgg.apps.googleusercontent.com`. It must be the
      **game server** OAuth client from §2a rather than the Android credential; the two are
      indistinguishable by sight, so §8's `(Google Play Games)` log line is what settles it.
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
costs one — `GameController.CommitLose()` calls `TrySpendLife()`, which is the only place in the
game a life is spent. Every give-up path funnels through `CommitLose`, so there is one hook, not
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

### Losing a level

Two conditions end a level, and each has its own popup:

| Condition | Trigger | Popup |
|---|---|---|
| Out of time | `_timer` reaches 0 → `ShowOutOfTime()` | `Out-Of-Time-Popup Variant` ("OUT OF TIME") |
| Out of moves | dead board survives every rescue → `ShowOutOfMove()` | `Out-Of-Move-Popup` ("GAME OVER") |

Both are the same component (`OutOfMovePopup`) — only the wording differs — and both offer a
revive for an ad or `ReviveCost` coins before anything is lost.

Leaving is a **two-step** flow, and the controller owns the routing:

```
out of time  ─┐                    ┌─ Leave  → CommitLose(): spend the life,
              ├─→ Lose-Popup Edited ┤          then SceneTransition → Home
out of moves ─┘   "lose your heart" └─ Cancel → ReopenRevivePopup(): back to
                                               whichever popup you came from
```

**`GameController._loseReason` is what Cancel reads**, and it is recorded at each point a loss
begins — not just when a revive popup opens. That distinction is the whole fix: a stuck board can
reach the confirmation *without any revive popup*, by declining the offer to open a basket
(`GiveUpStuckBoard`). Remembering the popup instead of the reason meant that route came back to
whatever was last on screen — after any earlier timeout, the out-of-time popup — or to nothing at
all on the first loss, dropping the player onto a dead board with the clock running.

**The life is spent in `CommitLose` and nowhere else**, so backing out is always free. Neither
revive popup's Leave button nor its corner X charges anything; both call `LeaveGiveUp()`, which
closes and hands over to `ShowLoseConfirm()`.

Both revive popups route their Leave through the controller rather than through a `PopupOpener` on
the button. A `PopupOpener` holds a prefab, not a reason, and it also skips `LeaveGiveUp`'s
`_resolved` latch — which left REVIVE tappable through the half-second closing animation.

### Input while a popup is open

The board is world-space colliders driven by `OnMouseDown`/`OnMouseDrag`/`OnMouseUp`, which Unity
dispatches by physics raycast. **A uGUI popup on top of it does not reliably stop those messages**,
so every board input handler asks `GameController.AcceptsBoardInput` first:

```csharp
gameStatus != win && gameStatus != lose && !Popup.AnyOpen && !RewardedAdController.IsShowingAd
```

Two traps that shape it:

- **It cannot be `gameStatus == play`.** A level sits in `pause` until the first drag, and it is
  that drag calling `DoStartTimer()` that starts it — so requiring `play` would deadlock the level.
- **Which is exactly why `Popup.AnyOpen` has to be in there.** `DoStartTimer()` sets the status back
  to `play` unconditionally, so dragging a dimsum through an open popup used to resume the level
  underneath it — and with the clock already at zero, `Update()` would re-fire `ShowOutOfTime()`
  and stack a fresh popup every frame. Tapping a closed basket through a popup likewise stacked the
  unlock offer on top.

`isTimerPause` is deliberately *not* consulted — the freeze power-up stops the clock while the
player keeps sorting. The guards fail **open** (`controller != null && !controller.Accepts…`) so a
missing injection can never leave the board unresponsive.

`OnMouseUp` is gated on `_moved` instead: Unity still delivers it for a press the handler ignored,
and ending a drag that never began would play the drop sound and tween the piece to a stale
`_initialPosition`.

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

## The board — pooling and rebuilds

Pieces (`MDimSum`) and plates (`MTray`) come from Zenject `MonoMemoryPool`s. **The pool only calls
`SetActive(false)`/`SetActive(true)`** — it restores nothing else. Everything an object's previous
life changed is still set when it comes back, which is what made items go missing "randomly": it
depends entirely on whether the pool hands you a fresh object or a used one.

Five separate causes, all fixed:

| Cause | Effect |
|---|---|
| A tray is faded to alpha 0 on its way out, then despawned. `MTray.Pool` had no `Reinitialize`. | Recycled plates came back **fully transparent**. |
| `CreateTray` fills the **last** tray (highest sorting order = front), but the refill callback filled `trayList[0]` (**back**). | The plate the player sees was blank. Only on baskets with **3+ trays** — with one or two the indices coincide. |
| That callback read `arrayDimsums[0]` unguarded. | Threw inside a DOTween callback on the final row. |
| `TotalFilledDimsums()` dereferenced null slots. | NRE unwound through `RemoveDimsum` and **aborted `OnEndDrag` before `DoDropPlaceAt`** — the dragged piece was never placed. Hit when a basket emptied with no trays left. |
| `BackToBottom()` rebuilds from a 2-second coroutine; nothing stopped a second one starting. | Overlapping rebuilds each despawned what the other had just made and each appended a fresh stack to `trayList`. |

That last one was reachable two ways: spamming the refresh power-up, and `HandleNoMoves`, whose
retry loop called `PowerUpRefeshItems()` and re-checked **in the same frame**. Since the rebuild
lands ~2s later, the check always re-read the old stuck board — so every attempt was burnt
instantly, one pending rebuild was stacked per basket per attempt, and the out-of-move popup
appeared even though a playable board was about to materialise. It is now the coroutine
`ReshuffleUntilPlayable`, which waits on `MDropArea.IsRebuilding` between attempts.

**Rule for anything pooled here:** reset it on spawn, and `DOKill()` its tweens. A `DOMove` left
running from the last drag will happily walk a piece off the slot it was just placed in.

### Dragging — the held piece grows

This is a phone game, so a piece under the fingertip is a piece the player cannot see. While a drag
is live, `MDimSum` grows it to **1.5x** (`dragScale`, a `[SerializeField]` on the dimsum prefab —
tune it in the Inspector, no code change). It tweens up over 0.12s on grab and back down over 0.15s
on release.

**Floating the piece above the finger was tried and removed.** Every offset large enough to
actually clear a fingertip made the piece feel unstuck from the thumb, and it moved the collider
with the sprite — which is what `OnTriggerEnter2D` and `MDropArea.CheckDropPosition` read, so the
piece resolved to a higher slot than it looked like it was going to. (`Settings.THRESHOLD_HEIGHT`,
0.7, is the top-slot/bottom-slot cutoff measured from the basket centre to the piece, so a lift
shifts it directly.) The grow gives the same visibility without ever separating what the player
sees from what they hit. **If you reintroduce a lift, the drop test has to be offset by the same
amount** or aiming will lie.

The scale tween is held in `_scaleTween` and killed before a new one starts. A second `DOScale`
does not replace the first, it just adds one, so re-grabbing a piece inside the drop's shrink would
otherwise leave two of them writing `localScale` every frame. `Reset` clears `_moved` and the tween
reference too, so a piece despawned mid-drag by a power-up cannot come back still believing it is
held — the same pooling trap as everything above.

## Power-ups — gating, spending, and the buy popup

Four power-ups sit in the top bar (`PlayTopBar`, inside `Panel-Game.prefab`). The button order is
not guessable from the numbers, so `GameController` names the slots:

| Slot | Button | Effect | Unlocks at | Needs a match? |
|---|---|---|---|---|
| 1 | Package | Packs away three matching dim sum | Lv. 7 | **yes** |
| 2 | Magnifier | Reveals and clears three matching dim sum | Lv. 2 | **yes** |
| 3 | Shuffle | Rearranges everything still on the plates | Lv. 14 | no |
| 4 | Extra time | Freezes the timer for 15s | Lv. 10 | no |

### Three gates, all greyed rather than hidden

`PlayTopBar.RefreshInteractable` sets `interactable` — never `SetActive` — so the row never
reshuffles under the player's thumb mid-level. A button lights up only when the level is unlocked,
the level is still running (not won or lost), **and**, for the package and the magnifier, the
board is actually holding three of a kind.

That last check goes through `GameController.HasReadyMatch`, which calls the very same
`GetDimsumReadyOnTop()` the two power-ups use to pick their targets. That is the point: **if the
button is lit, pressing it does something.** A cheaper re-implementation could drift from it and
put the player back to burning a power-up on an empty board.

It polls (`refreshInterval`, 0.15s) rather than waiting to be told, because the board changes
without the player touching a button — a basket completing, a refill landing, a rebuild finishing.
`HasReadyMatch` returns false when `_gameBaskets` is still null: the top bar's `Start` runs before
the controller has built the board, and without that guard it throws on the first frame.

### Spending, and running out

**Every power-up goes through `GameController.TryUsePowerup`**, which spends one or opens the buy
popup. There is no route that fires an effect without paying for it — before this, nothing ever
decremented `totalPowerup1..4` and the power-ups were effectively infinite.

`GameSetting` addresses the four counters by number (`GetPowerup`, `AddPowerup`,
`TrySpendPowerup`) so the UI can loop instead of repeating itself four times. Like `TrySpendGold`,
the check and the charge are one call, so a double tap cannot spend two.

Running out is **not** one of the interactable gates. An empty power-up stays pressable and opens
the shop, and the "+" badge on the button (shown only at zero) does the same.

### The buy popup

`Buy-Powerup-Popup.prefab` + `BuyPowerupPopup.cs`, duplicated from `Unlock-Basket-Popup` so the
two read as one shop: same layout, same two options — **500 coins** (`GameSetting.PowerupCost`) or
a rewarded ad — and the same greying when an option is unavailable.

It sells the one power-up the player reached for rather than a shelf, so `ShowBuyPowerupPopup(slot)`
hands it a slot and it dresses itself from the `entries` array on the prefab: title, one-sentence
description, and **the same icon the top-bar button uses**. Adding a fifth power-up is an Inspector
edit, not a code change.

Buying does not fire the power-up — the popup closes, the button lights up with its new count, and
the player spends it when they mean to.

### Gaining one — the icon flies down to the button

Modelled on `WinPopup.SpawnCoins`, which throws coins at the coin pill. On a gain, `PlayTopBar`
spawns one icon per power-up, kicks it away from where it came from, then flies it into the button
it belongs to (`flyDuration` 0.55s, all four numbers tunable on the prefab). **The button pops
(`DOPunchScale`) and the sound plays when the icon lands**, not when the coins were taken.

**It is driven by the top bar watching the counter, not by the popup announcing a sale.** That
covers every source — coins, a rewarded ad, an IAP bundle crediting all four at once — and nothing
has to remember to notify the bar. The popup's only contribution is
`PlayTopBar.SetGainOrigin(worldPos)`, a static it calls just before crediting so the icon appears
to leave the art the player is looking at. It is consumed by the next gain; unset means the middle
of the screen, which is all a bundle or an ad reward can claim anyway.

Details that matter:

- **The label holds its old value while the icon is in the air.** It prints
  `count - inFlight[slot]`, so the number ticks up on arrival — the same read as coins landing in
  the pill. The "+" badge uses the same held-back figure, so it clears on arrival too.
- The **first** read only records a baseline. Loading a save with three power-ups in it is not
  something the player just earned.
- **One sound however many counters moved**, carried by the last icon of a batch. A bundle credits
  all four, and four overlapping copies of one clip is a mess, not a fanfare.
- Flown icons are capped at `maxFlyIcons` (5) — a bundle can credit a lot — parented to the canvas
  root and pushed to the back of the sibling list so they pass *over* the popup, with
  `raycastTarget` off so they never eat a tap.
- The flight `Sequence` is given `SetTarget(rt).SetLink(flyer)`. **A bare `Sequence` belongs to
  nothing**, so DOTween cannot tell its object has gone and it outlives a scene change still
  driving a destroyed transform. The arrival callback also re-checks `this` for the same reason.
- The punch tween is **killed and the scale reset before a new one starts**. `DOPunchScale` springs
  back to whatever scale it captured on start, so a second pop landing on a running one would
  strand the button at the size the first had reached. Scale does not feed layout, so popping a
  button cannot shove the rest of the row sideways in the toolbar's `HorizontalLayoutGroup`.

The icon that flies is read off the button's own `Active/Icon`, never serialized separately, so the
thing that lands can never be a different power-up's art. Note the toolbar is **bottom**-anchored
despite the class being called `PlayTopBar`. The level is paused while the popup is up, restored in
`popup.onClose` so every exit is covered.

`popupBuyPowerup` is wired on the `GameController` in **all eight play scenes** (`Game` +
`Tutorial1..7`) and on `Ctrl.prefab`. The scenes' `Ctrl` objects are **not** prefab instances, so
each one has to be wired separately — setting it on `Ctrl.prefab` alone changes nothing.

## Profile

Tapping the avatar in Home's top bar opens `Edit-Profile-Popup SortFood`, which sets a **name**
and an **avatar**. Both live on `GameSetting`, so they persist to PlayerPrefs and ride along in
the Cloud Save snapshot with everything else — no separate sync.

- **Nothing is written until Save.** Cancel and the X leave the profile untouched.
- **The default name comes from Unity Authentication**: `Player-` plus the first four characters
  of the UGS player id, uppercased (`Player-X2JF`). Seeded by `GameSetting.EnsureProfile`, which
  runs on boot *after* the cloud pull — so a returning player's own name lands first and the
  default never overwrites it. The popup calls it again on open, covering the case where sign-in
  had not finished at boot.
- The popup's **ID** row shows the first 8 characters of the real player id, which is what the
  kit's `IXDA@DAY` placeholder was sized for.
- **The avatar is stored as the sprite's name**, not an index into the catalog — reordering or
  inserting an avatar would otherwise silently give every existing player a different face.
- A cloud snapshot only overwrites the profile when it actually carries one, so syncing against a
  save written before profiles existed cannot blank out a name the player just chose.

`Assets/ScriptObjects/AvatarCatalog.asset` lists the 15 avatars in the order the popup shows them.
Both the popup grid and the top bar resolve through it, so there is one list rather than two that
can drift. **Adding an avatar is two edits and no code**: add a toggle to the popup's grid
(`Content/Content/Content-Items/ScrollRect/Viewport/Content`) and the matching sprite to the
catalog, in the same position.

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

**It was generated once, and is maintained by hand now.** The generator
(`Assets/Editor/ShopTabBuilder.cs`) has been removed — it only ever ran at the start, and its
rebuild deleted and re-created the whole tab, which became a hazard once the rows were edited by
hand. If you ever want it back: `git show 5c232b5:Assets/Editor/ShopTabBuilder.cs`.

What it left behind is the part that matters. `Assets/Prefabs/UI/Generated/Shop/` holds the row
templates ("Generated" in the path is historical now):

| Prefab | What it is |
|---|---|
| `Shop-Section-Header.prefab` | The full-width section bar |
| `Shop-Bundle-Row.prefab` | One bundle row, every optional piece included |
| `Shop-Gold-Tile.prefab` | One gold tile |
| `Shop-Gold-Panel.prefab` | The 3-column grid the tiles sit in |

**The rows in the scene are real prefab instances of these**, so restyling every bundle at once
still means editing `Shop-Bundle-Row.prefab` — one edit, no code.

Alongside them, `Illustrations/` holds the **hand-made** animated art the No Ads cards use —
`Shop-Illustration-Sealbear.prefab` and `Shop-Illustration-Bunny-Mail.prefab`, lifted from the
kit's other panels. Placement lives inside the illustration prefab itself, so moving the art moves
with it, and a card showing one hides its flat icon.

Every amount printed on a card has to match `IAPCatalog.Rewards`, or the shelf advertises
something different from what the purchase grants — nothing enforces that now, so check it by eye.
Prices are the exception: never author them, `IAPBuyButton` fills them in from the store at
runtime in the player's own currency.

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

- **Layout and styling belong in the four prefabs, not in the scene.** Editing a card in
  `Shop-Generated` changes that one row; editing `Shop-Bundle-Row.prefab` changes all of them.
  Art added to a card directly in the scene is fine now that nothing regenerates the tab, but
  saving it under `Illustrations/` as its own prefab — the way the two No Ads illustrations are
  handled — keeps its placement with the art instead of in the scene.
- The ∞ on the unlimited-lives hearts is not in Dosis, so TMP falls back to LiberationSans and
  bakes the glyph on demand. That fallback is a **Dynamic** atlas with its source TTF referenced,
  so the glyph is produced at runtime — the committed atlas file is only a cache, and a diff
  appearing in it after a play session is noise, not a required asset change.
- **Adding a product is three steps, and nothing links them:** add it to `IAPCatalog.cs` (id,
  type, reward), duplicate a row in `Home.unity`'s shop page and point its `IAPBuyButton` at the
  new id, then create the product in Play Console. A product missing from any one of the three is
  silent — no card, or a card that cannot be bought.
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
  (`Coins-1-Chest` … `Coins-6` is a single coin), so the gold tiles run through them in reverse:
  the cheapest tier uses the highest-numbered sprite. Check the art against the tier when adding
  one — the numbering reads backwards from what you would expect.
- The background is the same scrolling tile prefab the other tabs use, in the shop's own colour:
  Home is `Backgrund-Tiles-Blue`, Missions `Backgrund-Tiles-Violet`, shop `Backgrund-Tiles-Green`.
- **Everything that used to open a currency popup now opens this tab.** Both top-bar pills carry
  `ShopTabButton`; `Coins-Shop-Popup` and `Lives-Shop-Popup` are unreferenced and can be deleted.
  Only the in-level popups (`Out-Of-Move-Popup`) still use `PopupOpener` for currency, because
  the shop tab does not exist in the Game scene — `ShopTab.Show()` logs a warning and no-ops
  wherever there is no PagedRect.

---

## Popups — the silent dead-button trap

`Unlock-Basket-Popup`'s close button did nothing. It was wired to **`LeaveGiveUp` on a null
target** — a leftover from the prefab being duplicated from `Out-Of-Move-Popup`. Re-rooting the
copy broke the reference, and **UnityEvent skips a persistent call whose target is missing without
a word**: no exception, no warning. The button still played its click sound, so it looked alive.

It is now `[Popup.Close] [UnlockBasketPopup.PlaySoundButton]`, matching every other popup.

All 58 prefabs containing a `Popup` were checked at the time of the fix; that was the only broken
one. **Check by hand after duplicating any kit popup** — that is how this one happened. Select the
button and read its On Click list in the Inspector: a dead entry shows the object slot as
`Missing` or `None`, and the method name greyed out. Two things not to mistake for a fault:

- A button with **no listeners at all** is often correct here. `UnlockBasketPopup` binds its two
  option buttons in `Awake` from serialized fields, and `RestorePurchasesButton` /
  `PrivacyOptionsButton` / `OpenUrlButton` bind themselves.
- **'Rate Us'** in `Settings-Popup Food Sort Home` really is dead, by omission — it is the
  unimplemented `rate us window` item from `missing.txt`.

It happened again, and not in a popup: all four **power-up buttons** in `Panel-Game.prefab` had a
dangling `PlayButtonClickClip` call, so they never played a click sound. They now point at
`PlayTopBar.PlayButtonSound`, which existed for exactly this and was going unused.

### Editing a prefab's On Click list without breaking the scene

Fixing those four is a trap worth writing down. **Never resize `m_OnClick.m_PersistentCalls.m_Calls`
on a prefab asset** — the scene's prefab-instance overrides are keyed on the *array index*, so
removing an entry silently drops the scene's own calls. Retarget in place instead: set `m_Target`,
`m_TargetAssemblyTypeName` and `m_MethodName` on the existing element and leave the array length
alone.

Two more things that make this confusing:

- A call pointing at a **scene** object (`GameController.PowerUpMagnifier`) reads as a **null
  target inside the prefab asset** — it is only resolvable in the scene. It is not broken; do not
  "clean it up".
- A scene can pin its own `m_Target` for an element, which **wins over the prefab**. Fixing the
  prefab is not enough — the scenes have to be fixed too. All eight were.

> **Testing popups over MCP:** the Editor does not tick frames while unfocused, so `Popup.Close()`
> appears to do nothing — its `WaitForSeconds` destroy coroutine never advances and `Time.frameCount`
> stays frozen. Set `Application.runInBackground = true` first, or you will chase a bug that is not
> there.

## Reference — what is already wired in Unity

| File / object | Role |
|---|---|
| `Assets/Scripts/Commons/IAPCatalog.cs` | The 15 products, types, and rewards. Single source of truth. |
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
| `Assets/Scripts/UI/HomeScene.cs` | Home top bar: gold, the heart's count / ∞ countdown, and the profile. |
| `Assets/Scripts/UI/ProfilePopup.cs` | Edit-profile popup: name and avatar, saved on Save only. |
| `Assets/Scripts/Commons/AvatarCatalog.cs` | The pickable avatars; `AvatarCatalog.asset` holds the list. |
| `Assets/Scripts/Commons/GameSetting.cs` | Save data, plus the life economy: cap, spend, settle, daily grant. |
| `Assets/Scripts/UI/PlayTopBar.cs` | The four power-up buttons: level / match / level-running gates, counts, and the "+" badge. |
| `Assets/Scripts/UI/BuyPowerupPopup.cs` | Sells one power-up for coins or a rewarded ad; dressed per slot from the prefab's `entries`. |
| `Assets/Prefabs/UI Popups/Buy-Powerup-Popup.prefab` | That popup. Wired on the `GameController` in all eight play scenes. |
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
