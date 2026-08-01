# Next Steps — work to do outside Unity

Everything still required to ship IAP, Google Play login, and compliant ads. The Unity-side
code and wiring is done; this file is the console/web work that has to happen before any of it
functions on a real device.

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

- [ ] **Create the 9 in-app products** in Play Console (§1). Until they exist and are Active, the
      shop shows `…` and every buy button stays disabled.
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

### Consumables (repeatable)

| # | Product ID | Suggested name | Grants | Suggested price |
|---|---|---|---|---|
| 1 | `coins_500` | Handful of Coins | +500 gold | Rp 15.000 |
| 2 | `coins_1200` | Bag of Coins | +1.200 gold | Rp 39.000 |
| 3 | `coins_3000` | Crate of Coins | +3.000 gold | Rp 79.000 |
| 4 | `coins_8000` | Vault of Coins | +8.000 gold | Rp 159.000 |
| 5 | `lives_refill` | Life Refill | +30 lives | Rp 15.000 |
| 6 | `powerup_bundle` | Power-Up Bundle | +5 of each power-up (1–4) | Rp 39.000 |
| 7 | `booster_bundle` | Booster Bundle | +5 of each booster (1–3) | Rp 39.000 |

### Non-consumables (bought once, restorable)

| # | Product ID | Suggested name | Grants | Suggested price |
|---|---|---|---|---|
| 8 | `remove_ads` | Remove Ads | Hides banner ads permanently | Rp 49.000 |
| 9 | `starter_pack` | Starter Pack | +1.000 gold, +10 lives, +3 of each power-up and booster | Rp 29.000 |

**Notes**

- [ ] Mark every product **Active**.
- [ ] The app needs at least one build uploaded to a track (Internal testing is fine) before the
      store returns prices. Products on a draft app report "unavailable".
- Prices are a starting point — the game never displays hard-coded prices, it shows the store's
  localized price string. Changing a price in the console needs no code change.
- Only 5 of the 9 have UI so far (4 coin tiers + `lives_refill`). Create all 9 anyway; the rest
  become purchasable the moment a button points at them.

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

- [ ] **Authentication** — enable. Required; player identity for IAP.
- [ ] **In-App Purchasing** — enable, and paste the **Google Play license key**
      (Play Console ▸ Monetize ▸ Monetization setup ▸ *Base64-encoded RSA public key*).
- Economy / Cloud Save are optional. The packages are installed but the code does not use them.

---

## 4. AdMob console

- [x] ~~App ID~~ — `ca-app-pub-8590881680208951~1081208993`, set in
      `GoogleMobileAdsSettings.asset` and confirmed written into the androidlib manifest.
- [x] ~~Ad units~~ — banner `…/8740207165`, rewarded `…/2518293601`, both live in-scene.
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

---

## 7. One-off steps back inside Unity

Small tasks that pair with the console work above, listed so nothing is missed:

- [ ] **Window ▸ Google Play Games ▸ Setup ▸ Android setup** → paste the Resources Definition XML
      from §2a and click Setup. Sign-in fails on device without it, even though the project
      compiles.
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
      validation (see Known gaps).

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
[Consent] Status=Obtained, canRequestAds=True
[IAP] 9 products ready.
```

Fewer than 9 products means an ID mismatch or an inactive product in the console.

**In the Editor:** IAP runs against a built-in fake store — clicking Buy pops a confirm dialog
and approving credits the reward for real, so the shop can be exercised without any console
setup. Play Games and UMP have no Editor implementation, so sign-in is always anonymous and the
consent form never appears there. **Play from `Splash`** — starting in Home skips the controllers
and leaves buy buttons disabled.

---

## Reference — what is already wired in Unity

| File / object | Role |
|---|---|
| `Assets/Scripts/Commons/IAPCatalog.cs` | The 9 products, types, and rewards. Single source of truth. |
| `Assets/Scripts/Controllers/IAPController.cs` | Store connection, two-step purchase flow, granting, restore, dedup ledger. |
| `Assets/Scripts/Controllers/GameServicesController.cs` | UGS init, Play Games sign-in with anonymous fallback, `LinkWithPlayGamesAsync`. |
| `Assets/Scripts/Controllers/ConsentController.cs` | UMP privacy flow; `WhenAdsAllowed()` gates ad SDK startup. |
| `Assets/Scripts/UI/IAPBuyButton.cs` | Buy button: localized price, availability, purchase. |
| `Assets/Scripts/UI/RestorePurchasesButton.cs` | Wires the previously dead "Restore Purchases" button. |
| `Assets/Scripts/UI/PrivacyOptionsButton.cs` | "Ad Privacy" entry; self-hides where not legally required. |
| `Assets/Scripts/UI/OpenUrlButton.cs` | Opens the privacy/terms URLs. |
| `Splash.unity` | `GameServices`, `Consent`, `IAP` singletons (`DontDestroyOnLoad`). Boot scene, build index 0. |
| `Home.unity` | `Credits-Coins` → Coins popup, `Credits-Life` → Lives popup (was wrongly Gems). |
| `Assets/Prefabs/UI Popups/Coins-Shop-Popup.prefab` | 4 coin tiers wired to IAP. |
| `Assets/Prefabs/UI Popups/Lives-Shop-Popup.prefab` | Lives shop wired to `lives_refill`. |
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
- **Rewards are local-only.** Gold and boosters live in PlayerPrefs, so a reinstall loses them.
  Cloud Save and Economy are installed but unused; now that players have a stable UGS id,
  moving balances server-side is straightforward.
- **`powerup_bundle`, `booster_bundle`, `remove_ads`, `starter_pack` have no UI.** In the
  catalog, purchasable as soon as a button points at them.
- **The Lives shop has one item.** To add tiers: add products to `IAPCatalog`, duplicate the card
  in `Lives-Shop-Popup.prefab`, and grow the popup root height (`ScrollRect height = root − 300`).
- **"Rate Us" is still unwired.** Add `OpenUrlButton` with
  `market://details?id=com.yourfavoritegamestudio.Jajanan` once the app is live.
- **The in-game Settings popup** (`Settings-Popup Food Sort.prefab`, shown during a level) has no
  Restore / Ad Privacy / legal links — only the Home one does.
- **`Splash` has a hidden `Button-Login`** opening a demo email/password popup. It was already
  inactive and does nothing; sign-in is automatic. Delete it, or repoint it at
  `GameServicesController.LinkWithPlayGamesAsync()` to let anonymous players upgrade.
