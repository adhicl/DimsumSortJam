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
- [ ] **Register the nine Analytics event schemas** (§3a). Version code 5 reported nothing
      because the client never started data collection; that is fixed for version code 6, which
      also sends nine custom events. The standard events will flow on the fix alone, but the
      custom ones stay invisible until their schemas exist in Event Manager.
- [ ] **Extend the privacy policy** (§5). The published page covers Google AdMob / advertising ID
      and Google Play Games, but **does not mention Unity Gaming Services (pseudonymous player id,
      Cloud Save), Unity Analytics (gameplay events), or Google Play Billing (purchase records)** —
      all of which the app uses. Play's Data safety review compares the form against the policy,
      and this gap is exactly the kind of mismatch that gets a submission rejected. The analytics
      line matters twice over now: the consent dialog tells the player their data is anonymous and
      points them at the policy, so the policy has to actually describe what is collected.

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
- [ ] **Analytics** — enable the service, then register the nine custom event schemas. Full
      procedure in §3a below. **This is the remaining blocker on analytics data**: the client
      sends all nine as of version code 6, but an event with no matching schema is rejected by
      validation and never reaches a report, which looks identical to the SDK being broken.

---

## 3a. Unity Cloud Dashboard — Analytics Event Manager

**Why this exists:** version code 5 sent nothing at all, because
`AnalyticsService.Instance.StartDataCollection()` was never called — since Analytics SDK 5.0 the
service ships inactive and collects nothing, not even the automatic standard events, until that
call is made. It is called now (`GameServicesController.StartAnalytics`). That fix alone gets the
**standard** events flowing. The **custom** events below additionally need schemas registered here
before they will chart.

Dashboard is at **https://cloud.unity.com** ▸ project *DimsumSortJam* ▸ **Analytics**. Exact menu
wording drifts between dashboard revisions — go by the names below, not by pixel position.

### Step 1 — turn the service on

Analytics ▸ **enable** for the project, if it is not already. Nothing needs pasting; the project is
already linked by `cloudProjectId 968e9456-…`, which is why Authentication and Cloud Save work.

### Step 2 — create the nine custom events

**Analytics ▸ Event Manager ▸ Custom Events ▸ create a new event.** Do this nine times, once per
row. For each one, enter the event name, then add every parameter listed with its type.

> **Names and parameter keys are case-sensitive and must match character for character.** They
> come from `Assets/Scripts/Commons/GameAnalytics.cs`, which is the only file in the game that
> spells them. A typo here does not error anywhere — the event simply never appears.

| # | Event name | Parameter | Type |
|---|---|---|---|
| 1 | `levelStarted` | `levelIndex` | Integer |
| 2 | `levelCompleted` | `levelIndex` | Integer |
| | | `durationSeconds` | Integer |
| | | `reviveCount` | Integer |
| 3 | `levelFailed` | `levelIndex` | Integer |
| | | `failReason` | String |
| | | `durationSeconds` | Integer |
| | | `reviveCount` | Integer |
| 4 | `levelRevived` | `levelIndex` | Integer |
| | | `reviveReason` | String |
| 5 | `boardReshuffled` | `levelIndex` | Integer |
| | | `attempts` | Integer |
| | | `rescued` | Boolean |
| 6 | `reviveOfferShown` | `levelIndex` | Integer |
| | | `offerReason` | String |
| 7 | `rewardedAdCompleted` | `placement` | String |
| 8 | `rewardedAdUnavailable` | `placement` | String |
| 9 | `iapPurchased` | `productID` | String |

> **`productID` ends in a capital `ID`** — it is the one key that does not follow the `levelIndex`
> lower-camel shape, so it is the easiest of the nine to mistype as `productId`. The client sends
> `productID`; the schema must say `productID`.

If the type dropdown uses different words than the table (`INT` / `STRING` / `BOOL` rather than
Integer / String / Boolean), pick the equivalent — the SDK sends C# `int`, `string` and `bool`.

**Values the string parameters can take**, so you can build segments without waiting to see them
arrive:

| Parameter | Possible values |
|---|---|
| `failReason`, `reviveReason`, `offerReason` | `outOfTime`, `outOfMoves` |
| `placement` | `continueGame`, `reviveOutOfMove`, `unlockBasket`, `buyPowerUp`, `doubleReward` |
| `productID` | the 15 IDs in §1 |

`levelIndex` is **0-based** — it is `GameSetting.currentLevel` as stored, so level 1 on screen
arrives as `levelIndex 0`. Add 1 in the dashboard if you chart it for anyone non-technical.

### Step 3 — publish the schemas, if your dashboard asks

Some dashboard revisions keep newly created events in a draft state until you explicitly save or
publish the set. If there is such a control, use it — a draft schema does not validate incoming
events.

### Step 4 — verify

In order of how fast they answer:

1. **Editor, immediately** — *Window ▸ Analytics ▸ Debug Panel* in Unity lists events the moment
   they are recorded, before any upload. This proves the **call site fires**. It says nothing
   about whether the schema is right, because it never leaves the machine.
2. **Device, within a minute** — logcat must show
   `[GameServices] Analytics data collection started.` If that line is missing, UGS init failed
   and nothing will ever be sent; no amount of dashboard work will help.
3. **Dashboard, hours later** — Analytics ▸ **Data Explorer** / event browser. Check the
   **Production** environment: the game never calls `SetEnvironmentName`, so everything lands in
   `production`, including Editor Play mode sessions (see the note under *Analytics events*).

> **Do not judge a build in the first hour.** UGS Analytics is not real-time and routinely takes
> several hours to surface events even when the whole pipeline is healthy. This is the single
> most common reason to conclude "it is still broken" while it is in fact working. Confirm with
> logcat first, then wait.

If an event never appears but logcat shows collection started, the schema is the suspect: check
the Event Manager entry for a name or parameter-key mismatch against `GameAnalytics.cs` before
touching any code.

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
      until it clears. **This is the first thing to check when the banner "rarely shows".**
      The client now self-heals from every failure it can see (below), but it cannot conjure
      fill: a new app under AdMob's *limited ad serving* period, or one not yet linked to its
      Play listing, gets a trickle of impressions no matter how correct the code is. Look for
      the *Ad serving limited* / *Getting ready* banners on the app's page, and link the app to
      the store listing once it is live. Until then, judge the code by logcat, not by whether an
      ad is on screen: `[BannerAd] Failed to load: … No fill` means the pipeline reached Google
      and Google said no — that is AdMob-side, not a bug.
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

**`GameController._loseReason` is what Cancel reads**, and it is stamped where the loss begins:
`ShowOutOfTime()` and `ShowOutOfMove()` each record it before opening their popup. Remembering the
popup instead of the reason meant Cancel came back to whatever was last on screen — after any
earlier timeout, the out-of-time popup — or to nothing at all on the first loss, dropping the
player onto a dead board with the clock running.

Every stuck-board loss now reaches the confirmation *through* the out-of-move popup, so no route
arrives with no reason recorded. There used to be one — declining the offer to open a basket went
straight to the confirmation — which is exactly what made recording the reason at the popup
unreliable.

**On this route the life is spent in `CommitLose`**, so backing out is always free. Neither
revive popup's Leave button nor its corner X charges anything; both call `LeaveGiveUp()`, which
closes and hands over to `ShowLoseConfirm()`.

**Walking out costs a life too** (2026-10-02). The three "you will lose 1 heart" popups —
`Alert Lose Heart QUIT` (in-level Settings → BACK TO HOME), `Alert Lose Heart Retry` (Settings →
RESTART) and `Alert Request Fail QUIT` (a customer ran out of patience) — all call
`AlertQuitPopup.QuitPopup()`, which now calls `GameController.QuitLevel()` before the scene
changes. Tutorials are charged the same as any level. `QuitLevel` logs `levelFailed` with reason
`"quit"`; `GameController._lifeCharged` makes sure one level can never cost two lives, whichever
route it leaves by. Before this, `QuitPopup` only had a `//reduce one health` placeholder, so
quitting and restarting were free.

The Home Settings popup also has a BACK TO HOME button that opens the same "lose 1 heart" popup.
There is no level to charge there, so nothing is spent, but the warning is wrong — hide that button.

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

### Possible change: charge at level start, refund on a win

**Not implemented — parked 2026-10-02.** Lives are currently charged on the way *out* of a level
(lose confirm, quit, restart). That leaves one hole: **force-closing the app mid-level costs
nothing**, because no popup ever runs. The usual fix is to take the life when the level starts
and hand it back when the level is won. Things to get right if this is ever picked up:

1. **Remove the exit charges.** `CommitLose` and `QuitLevel` must stop calling `TrySpendLife`, or
   a lost level costs two lives. Restart is the worst case: charged on the way out *and* on the
   reload.
2. **Charge inside the level, not on the Play button.** Several entry points skip
   `PlayButton`'s `CanStartLevel` gate: Settings → RESTART loads `Game` directly, `LoadingScene`
   boots early-level players straight into a tutorial scene, and `WinPopup` chains early levels
   into the next tutorial. Charging in `GameController` startup covers all of them. **Needs a
   decision:** what a level started with 0 lives does — send the player Home with
   `Out-Of-Lives-Popup`, or let that one through free.
3. **Refund only what was actually charged.** A level started during an unlimited window was
   free, so a win must not hand back a life (that would mint lives). Record whether the start
   charged; that record decides the refund. A window that expires mid-level does not make the
   level cost anything after the fact; one bought mid-level (IAP bundles) just means the win
   refunds as normal.
4. **Regen clock and cap.** Spending from 5 starts `nextLifeAt`; a refund back to 5 must stop it
   (`RefreshLives` already clears the clock at full). If a life regenerates mid-level, the refund
   is capped at `MaxLife` and that regenerated life is wasted — rare, since levels are short
   next to the regen period.
5. **Refund at the moment of winning, not on Claim.** Refunding in `WinPopup`'s claim would lose
   the life if the app is killed on the win screen. Refund once (latched) when the level is
   won, and `SaveData()` right away.
6. **Player-visible cost (the trade-off itself).** Any mid-level kill now costs a life —
   including Android killing the game in the background during a rewarded ad (common on low-end
   phones) and crashes. That is the price of closing the force-close hole; most lives-based
   games accept it.
7. **Cloud Save.** `ApplySnapshot` takes `totalLife` wholesale, so a snapshot uploaded mid-level
   carries the charged count. That is correct (the level was abandoned) and needs no new merge
   rule, but test it once across two devices.
8. **Analytics.** Abandoned levels then show up as a start with no complete/fail event, which you
   could track as their own metric.

Touches `GameController`, `GameSetting`, `AlertQuitPopup` and `LoseQuitPopup`.

### Still to do

- **No rewarded-ad route out.** `RewardedAdController` already exists for the win-screen double
  and the in-level revive; offering "watch an ad for a life" on the gate would be a small
  addition and softens the 30-minute wait.
- **The top bar shows the life count, not the regeneration timer.** There is one label on the
  heart and the ∞ countdown already owns it. If you want "3 · 12:41", the label needs splitting.
- **Force-closing mid-level costs no life.** See "Possible change: charge at level start, refund
  on a win" above.
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
- **'Rate Us'** in `Settings-Popup Food Sort Home` used to be dead by omission. It now carries a
  `PopupOpener` and opens `Rate-Us-Popup` (see "Rate Us" below).

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

## Rate Us

Settings ▸ **Rate Us** (`Settings-Popup Food Sort Home`, the Home one — the in-game Settings popup
has no such button) opens `Rate-Us-Popup` through a stock Ricimi `PopupOpener`, the same mechanism
the other popup buttons in that prefab use.

Five tappable stars, a line of copy that answers the tap, and **RATE** / **LATER**. RATE stays
greyed until a star is picked, so the player can see where the flow ends before committing.

**The stars submit nothing.** They are how the ask is framed, not a submission — which is also why
a low score goes to the same place rather than being quietly swallowed. RATE hands off to Google's
In-App Review flow (`com.google.play.review`, wrapped in `Controllers/InAppReview.cs`), and falls
back to the store listing when that cannot run.

Two properties of that API drive the design:

- **It can decide to show nothing.** Google quotas how often a player sees the card, and when it
  declines, the flow still reports success. A rating is indistinguishable from a no-op, so
  **nothing may ever be paid out for rating** and the copy must not promise a result.
- **The Editor build is a stub** that returns success immediately with no UI. `InAppReview.IsSupported`
  therefore reports false outside an Android device, so the Editor exercises the store-URL
  fallback instead of a button that silently appears to work.

`hasRatedGame` is set when RATE is pressed, *before* the flow runs, precisely because the outcome
is unknowable — someone who asked to rate should not be asked again either way.

### The automatic prompt

Beyond the Settings button, the game asks once on its own. `WinPopup` calls
`RateUsPopup.ArmAfterLevelWin()` on its way out, but only when the run actually ends on Home
(levels below 5 hand off to another tutorial scene), and `HomeScene` raises the popup ~1.5s later.
Asking on Home rather than over the win screen keeps it off the reward the player is still
collecting.

`GameSetting.CanShowRatePrompt` owns the whole rule: level **5+**, never rated, and past
`nextRatePromptAt`. Showing it calls `SnoozeRatePrompt()`, which pushes that a week out —
scheduled on *show*, not on dismissal, so Later, the X, and backing out of Play's own card all buy
the same quiet week. Both fields are saved to PlayerPrefs and carried in the cloud snapshot, where
`hasRatedGame` ORs and `nextRatePromptAt` takes the later value: rating on one device settles it
for the account, and hopping devices cannot shake a fresh prompt out of a week already served.

The arm is consumed even when the prompt is suppressed, so one win can only ever raise one ask. If
another popup is already up when the delay elapses it simply stands down and waits for the next
level.

Note `HomeScene` lives on **`SceneContext`**, outside the UI tree, so it cannot walk up to a
Canvas — it resolves the scene's canvas by name and skips the screen-space overlays other SDKs
inject, which is exactly the trap that put an earlier test popup on the wrong canvas.

The URL is built from `Application.identifier`, so it follows the app id with nothing to keep in
sync: `market://details?id=…` on device (straight into the Play app, no browser bounce) and the
`https://play.google.com/…` form in the Editor and everywhere else. `storeUrlOverride` on the
prefab points it somewhere else if needed.

### Fitting content into a kit popup

Worth knowing before restructuring any of these, because it cost a rebuild here. The kit's popups
leave **`Content` with a rect that is never really sized** — in the Game canvas its height computes
to *negative*. That is harmless while children are pinned by hand (which is how the kit authored
them) but leaves a layout group nothing to work in, and the first attempt here laid the stars out
inside a collapsed box.

Two things make the fix non-obvious:

- **The card is not the popup.** `Background`, the header and the button tray are each pinned to
  the popup root with fixed sizes, so their heights are the *same number of canvas units on every
  canvas* while the card itself grows. The clear band is 563 units down from the top and 780 up
  from the bottom, and those numbers hold everywhere.
- **The title pill hangs ~100 units below the header's rect.** Clearing the header rect is not
  enough — the pill's tail draws over anything placed there, which is exactly how the stars came
  out invisible while every rect measurement said they fitted.

The same prefab renders at very different sizes depending on the canvas it is opened on (Home's
canvas is 991x2092, the Game scene's 682x1440), so check a popup in the scene that actually opens
it. Rendering the canvas camera to a `RenderTexture` works when the Game view will not draw
(the Editor does not tick frames while unfocused) — that is how the invisible stars were caught.

All the copy uses TMP **auto-sizing** (message 24–46, buttons 24–42), so a longer line shrinks to
fit rather than overflowing the card.

## Analytics events

This is the **code** side. The console work — enabling the service and registering the schemas —
is **§3a**, and is still outstanding.

Version code 5 sent **nothing at all**, not even the automatic standard events, because
`StartDataCollection()` was never called; since Analytics SDK 5.0 the service ships inactive until
it is. `GameServicesController.StartAnalytics` now calls it right after UGS init and opens the gate
on `GameAnalytics`.

All custom events go through `Commons/GameAnalytics.cs`, the only file that names them. Two things
must both be true for an event to reach a chart:

1. The client calls it — **done**, call sites below.
2. A schema with the exact name and parameter keys exists in Event Manager — **not done**, §3a.

| Event | Parameters | Fired from |
|---|---|---|
| `levelStarted` | `levelIndex` int | `GameController.ResetGame` |
| `levelCompleted` | `levelIndex` int, `durationSeconds` int, `reviveCount` int | `OnFinishUpdateProgress`, when the win is decided |
| `levelFailed` | `levelIndex` int, `failReason` string, `durationSeconds` int, `reviveCount` int | `CommitLose` — the confirmed give-up only |
| `levelRevived` | `levelIndex` int, `reviveReason` string | `GrantRevive` |
| `boardReshuffled` | `levelIndex` int, `attempts` int, `rescued` bool | `ReshuffleUntilPlayable`, both outcomes |
| `reviveOfferShown` | `levelIndex` int, `offerReason` string | `ShowRevivePopup`, new offers only |
| `rewardedAdCompleted` | `placement` string | `RewardedAdController.ShowAd` reward callback |
| `rewardedAdUnavailable` | `placement` string | `ShowAd` when there is no fill (device only) |
| `iapPurchased` | `productID` string | `IAPController.OnPurchasePending`, inside the already-granted guard |

`levelIndex` is 0-based; the reason and placement strings are listed in §3a. All of them are
constants on `GameAnalytics`, so change them there rather than at a call site — and update the
Event Manager schema to match if you do.

Two deliberate choices worth keeping:

- **`levelFailed` is not the revive popup.** Opening the popup is `reviveOfferShown`; the level is
  only *failed* once the player confirms giving up. Pairing the two gives the offer's take rate,
  and counting the popup as a failure would have double-counted every rescued level.
- **`rewardedAdCompleted` fires in the reward callback, not next to `Show`.** A user who backs out
  of an ad never earns the reward, and counting the presentation would overstate it.

Verification steps are in §3a, step 4.

Note that **Editor Play mode sessions report into the same production environment as real
players** wherever consent resolves to granted — which, in the Editor, it always does (see
*Analytics consent*). If that starts skewing the numbers, gate `StartAnalytics` behind
`!Application.isEditor`. It is left on because the Debug Panel is the only practical way to verify
a new event.

---

## Analytics consent

**Nothing is collected until the player's answer says so.** `AnalyticsConsent` stores that answer
(PlayerPrefs `analytics_consent`), `GameServicesController` acts on it, and
`AnalyticsConsentPrompt` decides whether to ask. Three states, defaulting to Unknown — an
unanswered prompt collects nothing, so "we are collecting" is always a recorded decision rather
than the absence of one.

### Who gets asked

Only players in a region that requires a privacy prompt. Everyone else is opted in silently, which
is what the game did before consent existed and what `PrivacyOptionsButton` already does for the ad
form — it shows itself only where a form was required. The region comes from UMP
(`ConsentController.IsConsentRequiredRegion`), which reports `Required` or `Obtained` for the EEA,
UK, Switzerland and the regulated US states, and `NotRequired` for most of the world. Google makes
that determination, which is why it is read back off UMP rather than guessed from a locale.

> **This is Google's determination for *ad* consent.** It is the best regional signal in the
> project and the prompt is genuinely separate from the ad form, but pointing an ad-consent region
> check at an analytics question is a judgement call, not a legal opinion. If a lawyer reviews
> this, that is the line to show them. The stricter alternative is to ask everybody.

### The flow

1. UGS finishes initializing. If consent is already Granted, collection starts immediately — a
   returning player does not wait on UMP.
2. `AnalyticsConsentPrompt.AskIfNeeded()` waits for the UMP flow to resolve.
3. Answer already stored → nothing happens. Region not regulated → opted in, no dialog. Otherwise
   the prompt is shown.
4. The answer is stored and `AnalyticsConsent.OnChanged` fires, which starts collection or purges.

**Timing.** On the *first* launch, collection does not begin until step 3 — it waits on the UMP
round-trip, typically a second or two. Nothing is lost by that: the standard startup events
(`gameStarted`, `sessionStart`, `clientDevice`) are generated *by* `StartDataCollection`, not
before it, so they are simply timestamped a moment later. Custom events fired before that point
would be dropped, but reaching a level takes several seconds of navigation, so none are realistically
at risk. On *every later* launch the stored answer is read before UMP is consulted and collection
starts immediately after UGS init, with no wait at all.

**If the region cannot be determined** — the UMP update failed because the device is offline, or
because no consent message is published yet (§4a) — no answer is stored and nothing is collected
that session. The question stays open and is retried on the next launch. This is deliberate:
`ConsentController` resolves even when the update fails, leaving `ConsentStatus` at `Unknown`, and
`Unknown` must not be read as "not in the EEA". Treating the two alike would permanently opt in an
EEA player whose first launch happened to be offline, and they would never be asked again. The cost
is that a genuinely offline first session reports nothing.

### Withdrawing

**Settings ▸ Privacy options** reopens the ad consent form and then the analytics prompt behind it,
sequentially. Both privacy choices sit behind that one existing entry rather than getting a row
each — it is already visible in exactly the regions where a prompt is owed, and a player looking
for "Privacy options" is looking for all of it. The `alsoAskAnalyticsConsent` checkbox on
`PrivacyOptionsButton` controls this; **turning it off removes the only route to withdrawing
analytics consent** unless you wire a dedicated button.

Withdrawal calls `RequestDataDeletion()`, not `StopDataCollection()`. It disables collection *and*
erases what the backend already holds, is safe to call in any SDK state, and retries across
sessions if the player is offline. `StopDataCollection` would only stop the future, which is the
weaker reading of withdrawing consent. Re-granting works afterwards — the SDK resets its deletion
status on the next `StartDataCollection`.

### The prompt itself

A **native Android dialog** (`NativeDialog`), not a Unity prefab: it has to appear during Splash
before any game canvas exists, and a system dialog is what players expect a privacy question to
look like. To replace it with a styled popup, reimplement `NativeDialog.ShowConfirm` — nothing else
changes.

Android hands dialog callbacks back on its own UI thread, where PlayerPrefs and the Analytics SDK
are not safe to touch, so `MainThreadDispatcher` marshals the answer back to Unity's main thread.
It spawns itself on first use; there is nothing to place in a scene.

Where no native dialog exists the call returns a fallback answer instead of hanging: **false on
device** (no dialog means no informed answer, so never opt in), but **true in the Editor**, since
otherwise every event would be a silent no-op and the Debug Panel would be useless for verifying
call sites.

### Testing it

`AnalyticsConsent.Reset()` forgets the answer so the prompt appears again on the next launch. To
see the real dialog, you need a regulated region: set `debugGeography` to `EEA` on the
ConsentController in Splash and add your device's hashed id to `testDeviceHashedIds` (the Ads SDK
prints it in logcat). In the Editor the UMP bridge is a placeholder that always answers
`NotRequired`, so the dialog never appears there.

Verified in the Editor on 2026-09-11: grant → `[GameServices] Analytics data collection started.`,
deny → `[GameServices] Analytics stopped and data deletion requested.` and `GameAnalytics.IsReady`
false, re-grant → collection back on. Events recorded while denied are dropped without throwing.
**The native dialog itself is only exercisable on device** — the Editor never shows it.

---

## Ads — how the pipeline stays alive

Both ad controllers are `DontDestroyOnLoad` singletons in `Splash`, so structurally they were
already following the player through every scene. What made the banner *rarely* appear was
everything upstream of it being fragile: one bad network moment at launch and the session had no
ads, with nothing in logcat to say why. Four causes, all fixed on 2026-09-12:

| What went wrong | Where | What it did | Fix |
|---|---|---|---|
| **UMP lookup failed once → no ads all session.** `ConsentInformation.Update` was called exactly once; on failure `CanRequestAds()` read false and `WhenAdsAllowed` **dropped** the SDK-init action. `MobileAdsSdk._initializing` stayed true forever. | `ConsentController` | Every retry the banner and rewarded controllers have never ran, because the SDK they wait on never started. First launch on a flaky connection is exactly when this bit. | Failed lookups retry with backoff (10s → 60s cap). Init waiters are **held**, not dropped, and released the moment a later update says ads are allowed — including the player changing their answer in Privacy options. |
| **Threading rested on an obsolete flag.** `MobileAds.RaiseAdEventsOnUnityMainThread` is `[Obsolete]` in plugin 11.3.0. | `MobileAdsSdk`, both controllers | If a callback landed off-thread, `OnBannerAdLoaded → SafeAreaPanel` moved a RectTransform from a JNI thread, threw, and the exception was swallowed inside the callback. Timing-dependent, so it looked random. | Every ad and consent callback is marshalled explicitly through `MainThreadDispatcher.Run` — inline when already on the main thread, next frame otherwise. The flag is still set (harmless, `#pragma`-silenced) but nothing depends on it. |
| **No load watchdog.** A `LoadAd` that never called back — the SDK does not promise one — left the banner with no retry scheduled, and the rewarded `_isLoading` stuck true, which gates every future request. | Both controllers | Dead for the session, every "watch an ad" button greyed out. | 30s watchdog per request; no callback is treated as failure and enters the normal backoff. A late callback from a timed-out request keeps the ad if it is good and is otherwise ignored. |
| **No resume handling.** | Both controllers, `MobileAdsSdk` | Backgrounding the app mid-backoff meant waiting out the rest of it (up to 60s) after coming back — and coming back is precisely when connectivity changes. | `OnApplicationPause(false)` cancels the backoff and requests immediately. A loaded banner is re-shown, because some devices drop the ad's window on resume. `MobileAds.Initialize` gets its own 20s watchdog. |

### Verified

In the Editor on 2026-09-12, across every enabled build scene: Splash (hidden) → Home (shown) →
Game (shown) → Tutorial3 (shown) → Profile (hidden — not in `bannerScenes`) → Home (shown again).
The rewarded ad stayed ready throughout, and all three controllers survived every load. No ad
warnings, no exceptions.

**What the Editor cannot prove:** it runs the plugin's placeholder clients, which always fill,
always call back, and always deliver on the main thread. The retry, watchdog, and threading paths
only exercise on a device. The proof there is logcat:

- `[Consent] Update failed: …` followed later by `[Consent] Retrying consent lookup.` — the retry
  is alive.
- `[BannerAd] No response to load request; retrying.` / `[RewardedAd] No response …` — a watchdog
  caught a silent request.
- `[MobileAds] Initialize did not call back; retrying.` — the init watchdog fired.
- `[BannerAd] Failed to load: … No fill` — the code did its job; see §4 for the AdMob side.

### Where the banner shows

`BannerAdController.bannerScenes` on the Splash object: Home, Game, Tutorial1–7. It is hidden,
not destroyed, everywhere else, so it is back the instant one of those loads with no new request.
Add a scene name there to show it somewhere new. `Profile` is deliberately not in the list.

---

## Power-up row order

The four power-up buttons along the bottom read **left to right in unlock order**: magnifier
(Lv.2), package (Lv.7), hourglass (Lv.10), shuffle (Lv.14). Before 2026-09-25 they sat in slot
order — package, magnifier, shuffle, hourglass — so the first one a player unlocked was second
from the left and the row made no sense as a progression.

### Where the order lives

One place: the sibling order of the four children of `ScrollRect-Toolbar/Viewport/Content` in
**`Assets/Prefabs/UI/Panel-Game.prefab`**. That Content is a `HorizontalLayoutGroup`, so sibling
order *is* left-to-right order, and all eight play scenes (Game, Tutorial1–7) are instances of that
prefab with **no order overrides** — editing the prefab moves the row everywhere at once.

The slot numbering never changed. `powerUpBtn1` is still the package and `GameSetting.GetPowerup(1)`
still counts packages; only where they sit changed. Nothing that addresses a power-up by slot
needed touching.

### The tutorial covers are separate copies

Tutorial2, 5, 6 and 7 each introduce one power-up. Their `UseItem1` cover holds its **own duplicate**
of the toolbar — not a prefab instance — drawn above the dimming layer so the new power-up stays
lit while the rest of the screen goes dark, plus a `DownArrow` pointing at it. Those four copies
were reordered to match. **A change to the prefab row does not reach them**; they have to be done
by hand.

| Scene | Entered from level | Unlocks | Position in the row |
|---|---|---|---|
| Tutorial2 | 1 | power-up 2, magnifier (Lv.2) | 1st |
| Tutorial7 | 6 | power-up 1, package (Lv.7) | 2nd |
| Tutorial5 | 9 | power-up 4, hourglass (Lv.10) | 3rd |
| Tutorial6 | 13 | power-up 3, shuffle (Lv.14) | 4th |

### The arrow is placed by hand

Each cover's `DownArrow` is positioned manually in the editor (`Canvas/UseItem1/DownArrow`). The
runtime `TutorialPowerUpPointer` that used to re-centre it was removed on 2026-10-01. Caveat: the
toolbar stretches with the screen (`childForceExpandWidth`, CanvasScaler `match = 0.5`), so a
hand-placed arrow lines up exactly only at the aspect ratio it was authored at.

### If you change an unlock level

`Settings.minLevelPowerup1`..`4` are the source of truth, but the row order is *authored*, so
changing a constant silently leaves the row — and every tutorial arrow pointing into it — wrong.
`PlayTopBar.ValidateSlotOrder` catches that: an editor-only check that logs, on entering Play mode,
the exact order the row should be in. To fix, reorder the prefab's Content children, then the
duplicate row in each of the four covers, and move each cover's DownArrow by hand.

> **That warning is easy to miss in this project.** `Debug.LogWarning` does not come back through
> the MCP `read_console` tool here — `Debug.Log` does. Read it in the Unity console window, or
> capture it with `Application.logMessageReceived`.

### Verified 2026-09-25

All eight scenes report `Lv2 Lv7 Lv10 Lv14` left to right, for both the real row and the four cover
copies. In Tutorial7 at `currentLevel = 6` the arrow stayed exactly centred on the package
(0.00 offset) while the row was forced to widths 1075, 1775 and 2275 — the neighbouring slots moved
away proportionally, the arrow did not. The order guard was confirmed to fire once when the row is
scrambled and stay silent when it is correct.

---

## Power-ups — the empty-target crash

Fixed 2026-09-25. Symptom:

```
IndexOutOfRangeException: Index was outside the bounds of the array.
PowerUpAnimationEffect+<DoAnimateSuckPowerRoutine>d__15.MoveNext ()
  (at Assets/Scripts/GameObjects/PowerUpAnimationEffect.cs:85)
```

### What happened

`PowerUpSuckPackage` spent the power-up first and gathered its targets second, passing
`GetDimsumReadyOnTop()` straight through. That returns an **empty array** when the board holds no
triple, and a second later the routine read `positionDimsums[0]`.

The button is supposed to be greyed out in that state — `PlayTopBar` gates both match-driven
power-ups on `HasReadyMatch` — but it re-checks on a **0.15s poll**, not the instant the board
changes. So there is a ~150ms window after the last triple clears in which the button is still
live. A press landing there was all it took.

### Why it was worse than one exception

The throw happened *after* `DoAnimateSuckPower` had already switched the effect object on, and it
skipped the line at the end of the routine that switches it off. `GameController.PowerupRunning`
is nothing more than "is that object active", and `PlayTopBar` greys the entire row — all four
buttons and the "+" badges — while it is true.

So one mistimed press **cost a power-up and then killed every power-up button for the rest of the
level**. Confirmed live in the crashed session that reported it: `PowerupRunning=True`,
`HasReadyMatch=False`, effect object still active, all four buttons `interactable=False`.

### Closing the window that allowed it

The match gate itself was never missing — `PlayTopBar.RefreshInteractable` has always computed
`usable = unlocked && levelRunning && !powerUpRunning && (!NeedsMatch[i] || hasMatch)`, with
`NeedsMatch = { true, true, false, false }`: package and magnifier need a triple on the board, the
shuffle and the timer do not. What was missing was *immediacy*. That refresh only ran on the
0.15s poll, so the button stayed lit for up to a refresh interval after the board's last triple
went.

`RefreshInteractable` now runs **every frame**, outside the poll. The reason it did not before was
cost, and that turned out to be misjudged: `HasReadyMatch` measures **0.76 us per call** on a
30-slot board — about 0.005% of a frame at 60fps — so the poll was saving roughly nothing in
exchange for the hole it left. The counters stay on the poll, because those format strings and walk
the held/in-flight bookkeeping for a number that changes a handful of times a level.

Writes are guarded by `SetInteractable`, which skips the assignment when the value has not moved:
setting `Selectable.interactable` re-runs its colour transition either way, and this now touches
eight buttons every frame.

### The fix, in two layers

**Gather before spending** (`GameController.PowerUpSuckPackage`, `PowerUpMagnifier`). Targets are
collected first and the call returns early if there are none — the player is not charged for a
press the UI meant to refuse. The buy popup is unaffected: that is the separate "+" badge, which
never routed through here.

**The effect cannot lock the row** (`PowerUpAnimationEffect`). Independently of the caller:

- `DoAnimateSuckPower` / `DoAnimateMagnifier` refuse an empty or all-null list *before* switching
  the object on.
- The type is read from the first surviving target instead of element zero, and every loop skips
  entries that have gone — a second or more passes inside these routines and the board can clear a
  piece while they run.
- All four routines wrap their body in `try/finally` with the deactivation in the `finally`, so an
  exception can never again leave the row stuck. An exception thrown inside a coroutine unwinds
  through `MoveNext`, so the `finally` does run.

One bad frame should cost an animation, not the rest of the level.

### Verified

Against the fixed build, with no exceptions logged:

| Case | Result |
|---|---|
| `DoAnimateSuckPower(empty)` | no throw, effect never activates, `PowerupRunning=False` |
| `DoAnimateSuckPower(3 nulls)` | same |
| `DoAnimateMagnifier(empty)` | same |
| `PowerUpSuckPackage()` with no triple | package count unchanged — not charged |
| `PowerUpMagnifier()` with no triple | not charged |
| `PowerUpSuckPackage()` with a triple | charges 1, effect runs, and `PowerupRunning` returns to false when it ends |
| last triple removed | magnifier goes `interactable=False` on the **next frame** |
| triple restored | back to `interactable=True` on the next frame |
| poll blocked 9986s, button forced to the wrong value | corrected anyway — proves the per-frame path, not just the polled one |

The no-triple state was forced by nulling `_gameBaskets`, which is what makes `FindReadyMatchType`
return -1 — the same condition as the poll gap, without having to hand-play a board empty.

### Open: the unlock tutorials reveal a button that cannot be pressed

Found while verifying the above, **not fixed** — it is a separate bug and nobody has asked for it
yet.

Each unlock tutorial runs at the level *before* the one that unlocks its power-up: Tutorial7 is
played at `currentLevel = 6` and the package unlocks at `minLevelPowerup1 = 7`.
`CloseTutorialCover` calls `PlayTopBar.ShowTutorialPowerUp`, which swaps the padlock for the live
icon — but only the icon. `RefreshInteractable` still computes `unlocked = currentLevel >= minLevel`,
which is `6 >= 7`, so the button stays **non-interactable** for the whole tutorial.

Measured in Tutorial7 at `currentLevel = 6`: `activeRootShown=True`, `interactable=False`, with a
triple on the board. So the tutorial reveals the new power-up, points an arrow at it and captions
it "Clear 1 group of food", and the player cannot press it. It only becomes usable on the next
level.

The fix, when it is wanted, is to let the tutorial reveal count as unlocked for that slot — the
same condition `ShowTutorialPowerUp` already tests (`currentLevel == minLevel - 1`) — rather than
having the reveal and the gate disagree.

---

## The basket unlock offer

Fixed 2026-09-26. The offer was never appearing in the tutorial scenes: tapping a Closed basket
there just opened it, free and silently.

**Cause was scene wiring, not logic.** `GameController.ShowUnlockBasketPopup` falls back to
unlocking for free when `popupUnlockBasket` is unassigned — better than swallowing the tap — and
**all seven tutorial scenes had it `NULL`**. Only `Game` was wired. Three of those scenes actually
deal a Closed basket, so the bypass was reachable in normal play:

| Level | Scene | Baskets dealt (`LevelData.firstDisplayed`) |
|---|---|---|
| 6 | Tutorial7 | `DDDDDDDCDDLD` — one Closed |
| 9 | Tutorial5 | `DDDDDDDDDLCL` — one Closed |
| 13 | Tutorial6 | `LCLDDDDDDDDD` — one Closed |

All eight scenes now reference `Assets/Prefabs/UI Popups/Unlock-Basket-Popup.prefab`. Tutorial1–4
deal no Closed basket, so wiring them changes nothing today and stops the hole reopening if their
level data ever gains one. The fallback now logs a warning naming the scene, so a missing reference
announces itself instead of quietly giving baskets away.

### The "watch an ad" button was already gated

`AdRewardButton` on `Button-Use-Video` greys the option — `interactable = false`, alpha `0.45` —
whenever no rewarded ad is loaded, polling `RewardedAdController.IsAvailable` every 0.25s. It was
correctly wired in the prefab all along (`watchAdGate`); it simply never got the chance to run,
because the popup it lives on was never shown.

Two things to know when testing it:

- **In the Editor the button never greys.** `RewardedAdController.IsAvailable` is hardcoded `true`
  under `UNITY_EDITOR` so the three ad flows stay exercisable in Play mode, and `AdRewardButton`
  also treats a missing controller as available — which is what you get entering a gameplay scene
  directly, since the controller lives in Splash. On device neither applies and the gate reads
  `IsReady`.
- **The 0.25s poll is deliberate and fine here**, unlike the power-up row's. `IsReady` is a JNI hop
  on Android, and a press landing in the stale window costs nothing: `ShowAd` finds no ad, runs
  `onUnavailable`, and the gate re-greys itself. Nothing is spent and nothing throws.

## Character requests — no repeated item in one bubble

Fixed 2026-09-26. A character asking for two items showed the same dim sum twice.

`StartShowCharacter` took `N` items off one list. For `getOnTopOnly` requests that list came from
`GetDimsumReadyOnTop()`, which returns **the three pieces of a single matching type** — it is the
match-triple finder the magnifier and package use. Shuffling three identical types and taking two
returns that type twice, every time. Measured on the level 9 board: `[5,5]` on four runs out of
four.

The other branch was already fine — `GetAvailableDimsums()` is built on a dictionary keyed by type,
so it holds one entry per type.

**26 of the 223 authored requests are multi-item with `getOnTopOnly`**, so every one of them showed
a duplicate.

`PickRequestItems` now builds the order type by type. `getOnTopOnly` means "ask for something the
player can hand over right now", so the on-top type still **seeds** the request and that promise
holds for the first item; the rest of the board tops the order up with types not already asked for.
It also covers a case the old code got wrong in the other direction: `getOnTopOnly` with no triple
on the board used to hand the character an empty bubble, and now falls through to what is
available.

Single-item requests are unchanged by construction — one pick from the same source as before.

### Verified on the level 9 board

| Request | Result |
|---|---|
| `getOnTopOnly`, 2 items, x6 | `[5,7] [5,2] [5,7] [5,8] [5,2] [5,2]` — all unique, on-top type 5 always first |
| `getOnTopOnly`, 3 items, x3 | `[5,2,10] [5,8,7] [5,10,2]` — all unique |
| not `getOnTopOnly`, 3 items | unique, as before |
| `getOnTopOnly`, 1 item | `[5]` — identical to old behaviour |
| **old code, 2 items, x4** | `[5,5]` every time |

---

## Ads on an emulator

Live ad units **never fill on an emulator**. The Mobile Ads SDK classifies emulators as test
devices automatically, and a request against a live unit comes back empty - if one ever did fill,
the impression would be invalid traffic against the account. The symptom is a build where ads
simply never appear, with nothing obviously wrong in the project: App ID present in the manifest,
INTERNET granted, ad units configured, no errors.

`AdTestMode` now forces Google's sample units when it detects an emulator, checking the usual
`android.os.Build` fields (fingerprint, model, manufacturer, brand/device, product, hardware - no
single one is reliable alone). **Real hardware still honours the `useTestAd` checkbox**, so live
ads can still be verified on a device before shipping, and nobody has to remember to flip a toggle
back before release.

Both controllers now log which unit they ask for, so a build says plainly what it is doing:

```
[Ads] Emulator detected (Google sdk_gphone64_x86_64, hardware=ranchu); forcing Google's test ad units.
[BannerAd]   Requesting TEST banner ca-app-pub-3940256099942544/6300978111
[RewardedAd] Requesting TEST rewarded ca-app-pub-3940256099942544/5224354917
```

On a real device those read `Requesting LIVE …` with the project's own unit ids.

### Reading the outcome

With test units in play the pipeline is no longer in question, so what happens next is diagnostic:

- **Ads appear** → the client is fine end to end. Anything still missing with *live* units on real
  hardware is AdMob-side: app not yet Ready, limited ad serving on a new app, or genuinely no
  demand. See §4.
- **Ads still do not appear** → it is not AdMob. Check logcat for the request line above; if it is
  absent the controllers never ran, and if it is followed by a load failure the error code names
  the cause (3 = no fill, 2 = network, 1 = invalid request, 8 = missing App ID).

Capture with `adb logcat -s Unity:D Ads:V` while launching.

## Ads outside Splash

Fixed 2026-09-26. Starting play on any scene other than Splash gave **no ads at all** — no banner,
no rewarded video, and nothing in the console to explain it.

### Why

`ConsentController`, `BannerAdController` and `RewardedAdController` are DontDestroyOnLoad
singletons, but they were only ever *placed* in `Splash.unity`. Nothing else creates them. Start on
Home, Game or a tutorial — which is how a scene gets tested — and all three are simply absent:

```
scene=Home
Consent.Instance = False     ConsentController objects    = 0
Banner.Instance  = False     BannerAdController objects   = 0
Rewarded.Instance= False     RewardedAdController objects = 0
MobileAdsSdk.IsInitialized = False
```

Nothing calls `MobileAds.Initialize`, so the SDK never starts, and there is no controller alive to
log a complaint. It looks exactly like "the ads broke" when in fact the ad layer was never booted.
Note this is silent by design elsewhere too: `ConsentController.WhenAdsAllowed` warns when it finds
no controller, but that warning only fires if something asks — and with no ad controllers in the
scene, nothing does.

### The fix

`AdServicesBootstrap` spawns them from `Resources/AdServices` when they are missing. It runs at
`RuntimeInitializeLoadType.AfterSceneLoad` — *after* the first scene's Awake — so:

- **Booting from Splash is unchanged.** Those instances already exist by then, the bootstrap sees
  them and does nothing. Verified: Splash still uses its own `BannerAd` object and no clone appears.
- **Any other scene** gets one spawned copy, which lands in `DontDestroyOnLoad` like the originals.

It checks all three controllers, not just one: the controllers' own duplicate guards call
`Destroy(gameObject)`, and since the prefab carries all three components on a single root, spawning
on top of a half-populated scene would take the other two down with it.

The prefab is a **single root object carrying all three components**, deliberately. Each controller
calls `DontDestroyOnLoad(gameObject)` in Awake, and Unity only honours that for root objects — three
children under a shared parent would each warn and misbehave.

### Keeping the prefab honest

`Assets/Resources/AdServices.prefab` was generated **from the Splash objects** with
`ComponentUtility.CopyComponent` / `PasteComponentAsNew`, not configured by hand, so the live ad
unit ids and flags cannot drift from the ones that ship. Verified field by field against Splash at
creation: `ConsentController` 4/4, `BannerAdController` 13/13, `RewardedAdController` 2/2 matching,
zero differences — including `gameSetting`, `useTestAd = false` (live ads, not test), the nine
banner scenes, and `androidLiveAdUnitId = ca-app-pub-8590881680208951/2518293601`.

**If you change any of those on the Splash objects, rebuild the prefab**, or a scene-direct run will
quietly use the old values. Rebuilding is a copy of the three components from Splash onto a fresh
GameObject saved over that path.

### Verified

| Case | Result |
|---|---|
| Play **Home** directly | all three instances present, `IsInitialized=True`, `BannerHeightPixels=100`, rewarded `IsReady=True`, exactly 1 of each |
| Play **Splash** | unchanged — uses Splash's own `BannerAd`, no `AdServices` clone, 1 of each |
| Splash → Home transition | still 1 of each, banner 100px, rewarded ready |

### Boot waits for the services

`LoadingScene` used to hold the progress bar only for Cloud Save. It now waits on the whole boot
before leaving Splash, on two budgets:

- **Core services** — sign-in, Cloud Save, IAP, consent, and the ads SDK. Each either succeeds or
  gives up on its own, so waiting is bounded. A controller that is absent is skipped, so playing a
  scene directly still boots instead of sitting out the timeout.
- **Ad fill** — an actual banner and rewarded ad, waited on for `adFillTimeout` (4s) only. AdMob is
  entitled to return nothing at all, so fill gets a shorter, separate budget; without one, an
  account with no demand would add the full timeout to *every* launch.

`serviceTimeout` (8s) is a hard cap over both: a service that never answers delays the boot, it
cannot stop it. When the cap is hit, `ReportOutstanding` logs exactly which gates were late —
otherwise a slow launch and a broken service look identical from the loading screen.

Two supporting changes:

- `GameServicesController.SignInSettled` — `IsSignedIn` alone is unwaitable, because a *failed*
  sign-in leaves it false forever and the boot would sit out its whole timeout on every offline
  launch.
- `BannerAdController.IsLoaded` — `BannerHeightPixels` cannot answer "is the banner ready", because
  it is the *reserved* height and reads 0 in any scene that hides the banner. Splash is one of
  those, which is exactly where the loading screen has to ask.

`CanRequestAds()` is resolved once and cached. It runs from `Update`, and asking per frame spammed
the Editor console with `Placeholder ConsentInformationClient` every frame and cost a JNI hop per
frame on device.

The loading caption is now just `Loading` with animating dots. It used to print the player's
*current* level — not the one being opened — which meant nothing to anyone reading it.

### When the boot gets stuck

Before this, a wedged boot meant a loading bar sitting at 100% forever: no message, nothing to
press, force-close the only way out. `LoadingScene` now watches the whole boot and offers a way
out when it does not finish.

**Detection is deliberately generic.** Rather than enumerating what can hang - a service, ad fill,
the scene transition - the watchdog asks one question: *are we still on Splash after
`stuckTimeout` (20s)?* That catches every cause, including ones not thought of. It is checked
before the "transition already started" early-out, because the commonest stall is *after* the
transition begins with the scene never actually changing, and returning early there would leave
nothing watching. 20s sits comfortably above `serviceTimeout` (8s) plus the transition, so a merely
slow launch is never called a failure.

**The popup** is `Assets/Prefabs/UI Popups/Boot-Failed-Popup`, a Variant of
`Alert Lose Heart Retry`, so it keeps the game's art and follows it if that art changes. RETRY and
QUIT, and the close (X) is hidden on purpose - dismissing would drop the player back on a loading
bar that is never going to finish.

It is assigned to `LoadingScene.failurePopupPrefab` on the Splash object rather than loaded from a
Resources folder, so it can live wherever the other popups live and moving it cannot silently
break the failure path. Leave the field empty and the boot falls back to a plain system dialog.

> **All wording and artwork live on the prefab.** Edit the labels there; nothing in code writes
> them. `BootFailedPopup` used to push a title and message into the labels on enable, which meant
> the prefab showed one thing in the editor and something else at runtime, and every copy change
> was a code change. The component now serializes only the two buttons.

It builds **its own canvas** rather than borrowing one. Borrowing was wrong twice: the first canvas
found tends to be Ricimi's `TransitionCanvas`, which is *destroyed* when the fade ends and would
take the popup with it, and a boot that failed early may have no usable canvas at all. It also
creates an EventSystem if none exists - without one the buttons are decoration.

> `Popup.Open()` calls `SoundController.Instance.PlayOpenPopupClip()`, and **SoundController does
> not exist in Splash** - it lives in the game scenes. That NRE killed the whole popup and dropped
> the boot to the quit path. `Open()` is now attempted inside its own try/catch: what it adds
> beyond the sound is the dimming backdrop, which is a nicety, and the alert has to appear either
> way.

**RETRY genuinely retries.** Reloading Splash on its own would retry nothing: every service is a
`DontDestroyOnLoad` singleton, so it survives the reload, sees its own `Instance` already set and
destroys the fresh copy Splash just made - the stuck ones stay stuck. `BootRetry.TryRestart` tears
the six service objects down first (found by component, not by name, since they are named
differently depending on whether they came from Splash or `AdServicesBootstrap`), clears the
statics that would otherwise survive them, and only then reloads. Each class exposes its own
`ResetForRestart` for the parts that are private.

**QUIT, and the fallbacks.** Quit closes the app. If the styled popup cannot be shown at all, the
boot falls back to `NativeDialog`; where that cannot be shown either it answers false immediately,
which quits. The boot is dead in every one of those branches - the one outcome worth avoiding is
leaving the player on a frozen loading screen.

### Verified

| Case | Result |
|---|---|
| Boot wedged (`stuckTimeout` forced to 0.5s) | watchdog fires: `[Loading] Boot still on Splash after 0.5s; offering a retry.` |
| Popup content | title `CONNECTION PROBLEM`, correct message, `RETRY` + `QUIT` both interactable, close hidden |
| Popup canvas | its own `BootFailureCanvas`, sorting 32767, raycaster + EventSystem present; framebuffer shows the alert colours where Splash's blue would be |
| RETRY | tears down 6 service objects, resets the statics, reloads Splash; services come back |
| QUIT / native fallback | exits play mode via `QuitApp` |
| **Healthy boot** | reaches Home, no popup, no failure canvas — no false positive |

### The banner also died on every scene change (Editor only)

Separate bug, found straight after the above and fixed the same day. Booting from Splash, the
banner vanished the moment Home loaded — while the controller still believed it was up:

```
after Splash -> Home
  controller says BannerHeightPixels = 100     <- layout still reserving space
  controller _loaded = True
  placeholder DESTROYED by the scene change = True
```

In the Editor the "banner" is the plugin's placeholder: an ordinary GameObject parented into
**whichever scene was active when it loaded**. A scene change destroys it behind the plugin's back,
so `BannerAdController` goes on reserving the banner's height for something nobody can see. That is
why playing Home *directly* looked fine and the real boot path did not.

`KeepBannerAcrossScenes` now moves that placeholder into `DontDestroyOnLoad` the first time an ad
lands — **`#if UNITY_EDITOR` only**, and compiled out of player builds entirely. On device the
banner is a native view owned by the Activity and already survives; re-requesting it per scene (the
first attempt at this) would have thrown away a live impression and asked AdMob for fresh fill on
every screen.

It reaches into the plugin's internals by reflection to find the object, which is not nice but is
confined to the Editor and warns loudly if the field names ever change under a plugin update,
rather than letting the banner quietly start vanishing again. `DestroyBanner` also guards its
`Destroy()` call, since the view underneath may already be gone, and the "kept" flag resets on
`SubsystemRegistration` so it survives *Disable Domain Reload* being turned on.

Verified after Splash → Home: placeholder alive in `DontDestroyOnLoad`, `BannerHeightPixels = 100`,
exactly one banner object, and red pixels read back from the framebuffer inside the banner band.

> **The Editor placeholder is an untextured *white* bar** (`sprite = NULL`, colour white), sitting
> under white UI at the bottom of the screen — so it is very easy to mistake a working banner for a
> missing one. Two ways to check properly, both of which a normal screenshot gets wrong:
>
> - Screenshots taken **through a camera exclude Screen Space – Overlay canvases**, and the
>   placeholder's canvas is Overlay. It will not appear in a camera capture even when it is
>   rendering perfectly.
> - Sample the framebuffer instead: `ScreenCapture.CaptureScreenshotAsTexture()` then `GetPixel`
>   near the bottom edge. That is what confirmed this fix.

> **Scene transitions stall when the Game view is not rendering.** Ricimi's `Transition.RunFade`
> waits on `WaitForEndOfFrame` before calling `SceneManager.LoadScene`, and that never fires while
> the Game view is not drawing — an unfocused Editor being the usual cause. The symptom is Splash
> sitting there with `_transitioning = true` and `Transition` / `TransitionCanvas` parked in
> `DontDestroyOnLoad`. This is not a bug in the loading code: the pre-change `LoadingScene` stalls
> identically. It only shows up when driving the Editor without focus.

### Still Splash-only

`GameServicesController` (UGS auth + analytics), `IAPController` and `CloudSaveController` have the
same gap — play a scene directly and they do not exist either, so analytics records nothing and
there is no player id. They were left out on purpose: bootstrapping Cloud Save into an arbitrary
scene risks a sync against whatever local state that scene happens to start with. Add them to the
prefab if that trade stops mattering.

---

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
| `Assets/Scripts/UI/RateUsPopup.cs` | Star picker; in-app review with store fallback. Settings ▸ Rate Us, and the automatic prompt. |
| `Assets/Scripts/Controllers/InAppReview.cs` | Wraps `com.google.play.review`; false on anything but an Android device. |
| `Assets/Prefabs/UI Popups/Rate-Us-Popup.prefab` | That popup. |
| `Assets/Scripts/UI/VersionLabel.cs` | Prints `v1.2 (4)`; version code read from the installed package. |
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

## Android build & performance

Settled and version-controlled unless noted. The build is already IL2CPP / ARM64 / app bundle with
engine-code stripping on; what follows is what changed on top of that.

| Setting | Was | Now | Why |
|---|---|---|---|
| `Android.textureCompressionFormats` | **ETC** | **ASTC** | ETC1 has *no alpha channel*, so every transparent sprite — which is nearly all of them — was shipping uncompressed. The single biggest size win available here. |
| Accelerometer frequency | 60Hz | **0** | Nothing reads it. Polling a sensor 60x a second is pure battery. |
| Optimize Mesh Data | off | **on** | Drops mesh channels no material reads. Safe: everything here is sprites. |
| Quality *Medium* (Android's level) shadows | HardOnly | **Disable** | Nothing casts a shadow in a 2D game. |
| Quality *Medium* anisotropic | Enable | **Disable** | Only matters on surfaces viewed at a slant. |
| Frame rate | uncapped | **60** (`AppPerformance.cs`) | Left alone the game renders at the panel rate — 90/120Hz on many phones — for a board that is usually still. Up to half the GPU work and battery for nothing visible. |

**ASTC is safe here** because `minSdkVersion` is 26: every GPU shipping on Android 8+ supports it.

**Watch out:** the Build Settings *Texture Compression* dropdown (`EditorUserBuildSettings.androidBuildSubtarget`)
lives in `Library/`, which is **not** version-controlled — setting it there fixes nothing for anyone
else or for CI. `PlayerSettings.Android.textureCompressionFormats`, in the table above, is the one
that persists and the one the bundle actually uses.

`AppPerformance` applies the cap from a `[RuntimeInitializeOnLoadMethod]`, so there is no scene to
wire and no scene that can be forgotten. `targetFrameRate` is only honoured while VSync is off,
which is why it sets `vSyncCount = 0`; the Android compositor still presents on its own cadence, so
this does not tear the way it would on a desktop. It is `#if UNITY_ANDROID && !UNITY_EDITOR` — the
Editor's own pacing is more useful while working.

### Garbage, and why it mattered here

Three hot paths were rebuilding objects every frame or every poll. None of it was visible as a
frame-rate number; it shows up as periodic GC hitches on a mid-range phone.

| Path | Was | Now |
|---|---|---|
| `GameController.HasReadyMatch` | a `Dictionary` of `List`s plus an `int[]` per basket — **539 B per call**, polled ~7x/sec by the top bar | **0 B**, a nested scan over a board of a few dozen slots |
| `GameController.Progress` | a fresh string on every read, read **every frame** by the HUD | rebuilt only when the counter moves |
| HUD clock | a fresh string every frame for a clock that ticks once a second | rewritten only when the displayed second changes |
| Power-up counters | `$"{n:N0}"` per slot per poll | skipped while the number is unchanged |

Measured in play mode: **~105 B/frame → 0** for the HUD (~6 KB/s at 60fps) and **539 B → 0** per
match check.

`GetDimsumReadyOnTop` is now built on the same `FindReadyMatchType` the polled check uses, so the
lit button and the power-up cannot disagree about whether a match exists — verified over 600
randomised boards (217 with a match, 383 without, zero disagreements with the old algorithm). It
still allocates its three-item result, deliberately: the animation coroutine keeps that array alive
across frames, so it cannot be handed a shared buffer.

### The no-moves check was dead

Worth calling out separately, because it was not a performance problem — it was a correctness one
hiding inside the same code.

`GetDimsumsOnTop()` built a dictionary of every piece on the board, **threw it away**, and returned
`new MDimSum[3]` — an array of three nulls, always length 3. `HasAnyMove()` opened with
`if (GetDimsumsOnTop().Length > 0) return true;`, so it **always returned true on its first line**.
Everything after it — the empty-basket arithmetic that is the actual "can anything still move"
test — was unreachable.

Two consequences, both silent:

- `CheckIsGameNoMove()` is `if (HasAnyMove()) return;`, so **`HandleNoMoves()` could never run**:
  no unlock-basket offer, no reshuffle, no out-of-move popup, however stuck the board was.
- `ReshuffleUntilPlayable` re-checks `HasAnyMove()` between attempts, so it always concluded the
  first shuffle had worked.

`HasAnyMove()` now opens with `HasReadyMatch`, which is what the comment beside it always claimed
it did, and `GetDimsumsOnTop` is gone. Verified in play mode: a full board of all-distinct types
now reports `HasAnyMove=False`, and adding one triple flips it back to true. **This turns the
whole out-of-moves flow on for the first time** — worth playing a stuck board once to see it fire.

### ...and turning it on broke finishing a level

Switching the rescue on for the first time exposed four defects that had never been able to run.
The symptom was that **finishing a level** popped the unlock-basket offer or silently ran the
shuffle power-up, with errors in the console. All four are fixed; they are recorded because each
one is a trap that could be walked back into.

**1. The check ran mid-drop, at a moment the board is never really in.** `OnEndDrag` removes the
piece from its old basket and only places it in the new one on the *next line*:

```csharp
_prevDropAt.RemoveDimsum(_prevDropAtIndex, _dropAt);   // fired the no-move check
DoDropPlaceAt(_dropAt, indexPos, true);                // the piece finally lands
```

`RemoveDimsum` called the check the instant a basket emptied, so the board was judged with the
dragged piece on **no basket at all**. On the move that finishes a level the rest of the board is
already cleared, so it correctly answered "no moves left" about a board one frame from winning.

The fix is `GameController.RequestNoMoveCheck()` — everything in play calls that now, never
`CheckIsGameNoMove()` directly. It defers a frame so the drop lands, then waits out any animation
(`BoardIsBusy()`), then re-checks. Guarding alone would not have worked: at the moment the request
is made the level is *not yet* decided — `DoAddProgress` has not run. Only the deferral gets the
ordering right.

**2. `_awardedTotal`, because `_currentTotal` lies for half a second.** The win is declared by the
`DoAddProgress` tween landing, so for 0.5s after the winning match `gameStatus` still reads `play`.
`_awardedTotal` is incremented synchronously, and `LevelDecided` reads it. Anything asking "is this
level over" must use `LevelDecided`, not `gameStatus`.

**3. The rescue was spending the player's power-up behind their back.** `HandleNoMoves()`
reshuffled by calling `PowerUpRefeshItems()` — which spends a refresh power-up. So the game's own
rescue **took the player's power-up without asking**, and once they had none it opened the *buy
popup* instead, shuffled nothing, and burnt all five attempts against an untouched board.

The mechanic is now split from the price: `ReshuffleBoard()` is the rearrangement on its own, and
`PowerUpRefeshItems()` is `TryUsePowerup` + sound + `ReshuffleBoard()`. **Never call a
`PowerUpXxx()` method from game logic** — those are button handlers and they charge.

**4. The reshuffle walked three different basket lists.** It gathered leftovers from all of
`baskets`, reset `_gameBaskets`, then redistributed across all of `baskets` again. `SetUpBaskets`
deactivates the spares a smaller level does not need **but never clears their open flag** — verified
live: `PlaceDrop (10)` and `(11)` sit there `active=False, open=Displayed`. The redistribute loop
therefore called `BackToBottom` on an inactive object and `StartCoroutine` threw. Gathering from
baskets that are never rebuilt also duplicated their pieces into the ones that are.

All three steps now walk `GetBasketsToRebuild()` — `_gameBaskets` filtered to `Displayed` **and**
`activeInHierarchy`. A basket's open flag is not proof it is in play; check `activeInHierarchy` too.

#### A throw inside a coroutine is worse than it looks

Two of these produced `ArgumentOutOfRangeException` — `CreateDimsum` indexing `arrayDimsums[0]` on
an empty deal, and `CreateDimsumFromTray` indexing `trayList[^1]` after `DrawToTop` emptied it
(the rebuild only refills it 2s later, so a basket completing a match inside that window has rows
waiting and no plate to deal them from).

The throw is the real damage, not the error line. It unwinds the coroutine **before the flag at
the bottom is cleared**, so `_rebuildRoutine` / `IsClearing` stay set forever — and
`ReshuffleUntilPlayable` and `CheckNoMoveWhenSettled` both *wait on those flags in a loop*. One
exception permanently wedged the rescue for the rest of the level. Both routines now clear their
flag in a `finally`, and both indexers are guarded.

**Any coroutine that raises a "busy" flag must lower it in a `finally`.**

### A stuck board is sold a rescue, never given one

There is **no free reshuffle**. When the board runs out of moves mid-level, `HandleNoMoves()` puts
two things up for sale, and offers them in this order:

1. **A closed basket**, if any is left — `ShowUnlockBasketPopup`, paid with coins or a rewarded ad.
   Space is the real problem, so opening a basket is the fix that fits.
2. **A reshuffle** — `ShowOutOfMove()` opens `Out-Of-Move-Popup`, and paying there is what
   rearranges the board. Reached either because no basket is left to sell, or because the player
   turned the basket down: the decline handler passed to `ShowUnlockBasketPopup` **is**
   `ShowOutOfMove`.

Turning down the basket is therefore not giving up — it is turning down one of two offers. Only the
out-of-move popup's own Leave button ends the level, which keeps every way of losing a stuck board
on a single route (and is why `_loseReason` is always set by the time the confirmation opens).

`HandleNoMoves()` never reshuffles by itself. It only opens a popup.

Note that the basket for sale is `DisplayedBasket.Closed`. `DisplayedBasket.Locked` is the other
kind, which opens by matching the dim sum printed on it and is never sold, so
`HasClosedBasketLeft()` deliberately ignores it.

#### What counts as a move

`HasAnyMove()` answers one question: **can the player still clear a basket that has plates under
it?** A basket only gives up the plate it is standing on — and deals the next one — once its last
dim sum has been dragged off it, so that is the move the game is made of. Three in a row is one way
to get there, not the definition.

Two rules do all the work:

- **Only `Displayed` baskets are space.** A `Closed` basket has to be bought open, a `Locked` one
  opens by matching its printed dim sum, and `CheckDropPosition` refuses a drop on either. Neither
  is ever dealt any dim sums, so both used to read as **three free slots apiece** — two closed
  baskets were enough to convince the game the board always had a move, and the stuck-board offer
  never appeared at all. `IsPlayableBasket` also checks `activeInHierarchy`, because `SetUpBaskets`
  deactivates the spares a smaller level does not need without touching their open flag.
- **Space adds up across baskets.** Emptying a basket holding two pieces needs two free slots
  *somewhere*, not one basket with exactly two holes — which is what the old check demanded, and
  why it called perfectly playable boards dead. The basket's own holes are excluded: they are no
  help in emptying it.

The match route is kept, but it is space-aware. `HasReadyMatch` answers "three of these exist
somewhere", which is the right question for lighting the magnifier button and the wrong one here:
on a board with no hole left in it those three can never be gathered. `CanGatherThree` adds the
missing half. Gathering onto the basket that already holds the most of a type is always the
cheapest route — it needs room for the ones it is missing, and evicting its other pieces frees
exactly that much room, so the only thing that can block the match is having nowhere to evict to.
Write that out and the host's own holes cancel, leaving `free + held >= 3`.

#### What a revive grants depends on how the level was failing

Both revive popups (`Out-Of-Move-Popup`, `Out-Of-Time-Popup Variant`) share one script,
`OutOfMovePopup.cs`. It does not decide what the player gets — it calls
`GameController.GrantRevive()`, which reads `_loseReason`:

| Failure | Granted |
|---|---|
| `OutOfTime` | `ReviveWithTime()` — +45s on the clock |
| `OutOfMoves` | `ReviveWithReshuffle()` — the board is rearranged until it has a move |

That split is the whole point: **adding seconds to a board with no legal move sells the player
nothing**, which is exactly what the shared popup used to do. Keep new revive routes going through
`GrantRevive()` rather than calling `ReviveWithTime()` directly.

The out-of-move popup needed no art change — it already reads "GAME OVER / No more move available /
REVIVE", and the coin cost is `GameSetting.ReviveCost` (600), written into the label on open.

#### Two edges worth keeping

- **A bad opening deal is not a sale.** `CreateLevel` calls `CheckInitialDealPlayable()`, not the
  in-play check. The player has not touched anything yet, so a dead deal is the game correcting
  itself: it reshuffles quietly and, if even that fails, leaves the board rather than opening
  "GAME OVER" over a level nobody has played.
- **Paying once is paying once.** `ReshuffleUntilPlayable(loseIfImpossible)` takes a flag for this.
  After a paid revive it retries up to `maxReshuffleAttempts`, and if the pieces genuinely cannot
  form a move it goes to the lose confirmation — deliberately **not** back to `ShowOutOfMove()`,
  which would charge a second time for the same rescue.

### The audio mixer is the game's own asset now, not Feel's

Muting runs entirely through an `AudioMixer`. `GameSetting.soundMute` / `musicMute` are saved and
reloaded, but **nothing else in the game reads them** — no `AudioSource.mute`, no volume field — so
the mixer is not one way of silencing the game, it is the only one.

The mixer the game had always used was **Feel's** `MMSoundManagerAudioMixer.mixer`, and the
"clean up Feel" commit (`80fc7fee`) deleted it along with the rest of the package. Nothing failed at
build time, because a missing asset reference is a serialized GUID that no longer resolves. It
failed at runtime instead:

```
MissingReferenceException: The variable mixer of LoadingScene doesn't exist anymore.
MissingReferenceException: The variable mixer of SettingPopupHome doesn't exist anymore.
  at UnityEngine.Audio.AudioMixer.SetFloat
```

It is restored as **`Assets/Sounds/GameAudioMixer.mixer`** — the same asset, byte for byte, apart
from its name. Putting it back under `Assets/Feel` would only queue the same deletion up again.

The YAML was copied from the commit before the deletion, so every internal fileID (the Master,
Music, Sfx and UI groups, the snapshot, the effects) and every exposed-parameter GUID is unchanged.
Only the *asset* GUID differs, which made repointing the 12 referencing files a plain GUID
substitution with no fileID remapping: 8 scenes carry an AudioSource on the Music group, `Sound.prefab`
carries one on Sfx, and the three `mixer` fields point at the asset itself.

**A third-party package folder is not a home for something the game depends on.** If an asset from
one is load-bearing, copy it into the project's own folders and reference the copy.

#### The failure was silent for exactly as long as the contract was anonymous

The parameter names and the decibel convention were string literals written out six times across
three scripts. Nothing was named, so nothing noticed when the thing behind the names disappeared.
`Commons.AudioMix` now holds the contract — `SfxVolumeParam`, `MusicVolumeParam`, and what "muted"
means in dB — and the three call sites go through it.

It also reports a missing mixer instead of throwing, which is worth more than the audio:

| Caller | What the exception used to cost |
|---|---|
| `LoadingScene.DoLoading` | aborted the coroutine, so the animated "Loading..." text never ran |
| `SettingPopup` / `SettingPopupHome` | unwound past `SaveData()`, so the toggle neither applied nor persisted |

Note `mixer == null` rather than `is null` in `AudioMix.SetVolume`. A reference to a deleted asset
is not a real null, and only Unity's overloaded comparison recognises it — which is precisely the
case that has to be caught here.

### External Dependency Manager — installed once, via UPM

Unity used to warn on every reload that `Google.IOSResolver.dll` "will not be loaded". Harmless in
itself — it is the CocoaPods half of EDM4U, and **iOS Build Support is not installed** (the editor
has `AndroidPlayer`, `WebGLSupport`, `windowsstandalonesupport` only), so the reference could not
resolve. But it was the symptom of something real: **EDM4U was installed twice.**

| Copy | Version | Source |
|---|---|---|
| `Assets/ExternalDependencyManager/` | 1.2.188 | a `.unitypackage` (AdMob / Play Games bundle it) |
| `Library/PackageCache/…` | 1.2.185 | OpenUPM, transitive dep of `com.google.play.core` |

Only the older one warned, and the reason is the fix: **1.2.188 ships `validateReferences: 0` in
its `.meta`; 1.2.185 does not.** Google had already solved it upstream. The message's own advice —
"disable reference validation in the Plugin Inspector" — is impossible to follow for that copy,
because it lives in `Library/PackageCache/`, which is immutable: the Inspector is read-only there
and anything forced in is wiped on the next resolve.

Now: `com.google.external-dependency-manager` is pinned to **1.2.188** in `Packages/manifest.json`
(it was only a depth-2 transitive dependency, which is why UPM had settled on the older one), and
`Assets/ExternalDependencyManager/` is deleted. One install, no warning.

**If you ever delete a duplicate plugin folder, force-reimport the survivor.** Both copies shared
the same GUIDs. After the delete, Unity still had those GUIDs registered to the *deleted* `Assets/`
paths, so the package DLLs had **no importer at all** — they were being rejected as duplicates.
`Google.JarResolver` therefore never reached `Assembly-CSharp-Editor`, and two GoogleMobileAds
editor scripts failed with `CS0246: GooglePlayServices could not be found`. An
`AssetDatabase.ImportAsset(..., ForceUpdate | ImportRecursive)` on the package folder released the
GUIDs and everything linked up. A clean console is not proof on its own here — check that
`GooglePlayServices.PlayServicesResolver` actually resolves.

Verified after the change: 0 console errors, `Google.IOSResolver` **loads** (it did not before), all
EDM4U assemblies present exactly once, and **Android Resolver ▸ Force Resolve** leaves
`ProjectSettings/AndroidResolverDependencies.xml` and `Assets/Plugins/Android/` byte-identical —
same 9 packages, `play-services-ads`, `play-services-games-v2`, `play:review` and the rest.

### Still on the table

- **R8 / minification is off.** Worth turning on for a smaller DEX, but it needs a real device
  smoke test — Play Games, AdMob, IAP and Play Review all rely on reflection, and a missing
  keep-rule shows up as a crash in the release build only.
- **Managed stripping is `Minimal`.** `Low` is Unity's default and would strip more, but Zenject
  resolves by reflection, so anything above `Minimal` wants a `link.xml` and a device test.
- ~~`Assets/Feel/`~~ **deleted** — 4,913 files, 365MB of demo content (a 34MB and a 30MB `.wav`
  among them). Nothing in `Assets/Scripts`, any build scene, any prefab or any asmdef referenced
  it, and every GUID in it was checked against the rest of the project before it went. It never
  shipped in the bundle — unreferenced assets outside `Resources/` are not built — so this buys
  import time and repo size rather than APK size. `NiceVibrations` (haptics) went with it; nothing
  called it. Recover with `git checkout HEAD -- Assets/Feel` while the deletion is uncommitted.
- **The HUD timer colours are wrong on purpose-looking numbers.** `new Color(219f, 219f, 219f, 1f)`
  and `new Color(0f, 219f, 59f, 1f)` are 0-255 values in a constructor that wants 0-1, so they
  saturate: the paused clock draws white and the running one cyan, not grey and green. Left exactly
  as authored — dividing by 255 changes how the game looks, which is an art call.

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
- **A rating can never be confirmed.** Play's In-App Review card is quota-limited and reports
  success whether or not it appeared, so the game cannot know a review was left — never reward it.
  See "Rate Us" below.
- **The in-game Settings popup** (`Settings-Popup Food Sort.prefab`, shown during a level) has no
  Restore / Ad Privacy / legal links — only the Home one does.
- **`Splash` has a hidden `Button-Login`** opening a demo email/password popup. It was already
  inactive and does nothing; sign-in is automatic. Delete it, or repoint it at
  `GameServicesController.LinkWithPlayGamesAsync()` to let anonymous players upgrade.
