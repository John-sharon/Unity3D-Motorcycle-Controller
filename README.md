# Unity3D Motorcycle Controller

A starter project for a physics-based motorcycle game in Unity.

## Original assets and credits
- animanyarty (Motorcycle model)  
  https://sketchfab.com/3d-models/motorcycle-38404e2077ca4b209cd2f1db30541b94
- Subhash5922j (Biker model)  
  https://sketchfab.com/3d-models/racer-bikecar-9a7eae7771f94572aa72ca153153f340

---

## Turning this prototype into a hill-climb game

This repo already has:
- basic bike movement (`Assets/Scripts/MotorcycleController.cs`)
- obstacle crash trigger (`OnCollisionEnter` with `Obstacle` tag)
- rider ragdoll activation

Use the steps below to evolve it into a full game loop.

## 1) Define the core gameplay loop

1. **Ride uphill** while managing traction, throttle, and balance.
2. **Avoid obstacles** and rough terrain.
3. **Crash state**: rider separates from bike and enters ragdoll.
4. **Post-crash dodge state**: player controls ragdoll movement to avoid the falling bike.
5. **Bike hits rider = elimination**.
6. Run ends and awards money based on climb distance.
7. Spend money on upgrades/repairs before next run.

This gives you a complete roguelike loop: run -> reward -> improve -> run again.

## 2) Set up game states (recommended architecture)

Create a lightweight state machine with an enum (example):

- `PreRun`
- `Riding`
- `RiderEjected`
- `DodgingFallingBike`
- `RunComplete`
- `GameOver`

Keep state ownership in a central `GameManager` so `MotorcycleController`, scoring, UI, and economy all read one source of truth.

## 3) Improve bike physics for hill climbing

### Terrain and traction
- Use a long uphill terrain with varying slope angles and small bumps.
- Tune `WheelCollider` values:
  - forward friction stiffness (for climbing)
  - sideways friction (for drift/slide control)
  - suspension distance and spring/damper
- Add a center-of-mass offset to the bike rigidbody to reduce unrealistic flipping.

### Input and balancing
- Keep throttle/brake from `VerticalMove()`.
- Add pitch control (lean forward/back) by applying torque to bike rigidbody.
- Reduce steering effect at low speed/high slope to avoid twitching.

### Difficulty curve
- Increase slope steepness over distance.
- Spawn denser obstacles as the player climbs.
- Add occasional traction-loss zones (mud/loose gravel materials).

## 4) Crash and "dodge the falling bike" mechanic

You already activate ragdoll on obstacle collision. Extend it:

### Separation flow
1. Disable bike input when crash begins.
2. Detach rider from bike and enable rider ragdoll colliders/rigidbodies.
3. Apply an impulse to rider and bike in different directions.

### Dodge phase
- During `DodgingFallingBike`, let player influence rider with limited control:
  - left/right impulse
  - small roll/jump impulse (optional)
- Add a short time window (e.g. 2-4 seconds).

### Elimination rule
- Add a "danger" collider on the bike body.
- If bike danger collider hits rider root collider during dodge phase -> `GameOver`.
- If timer expires without collision -> `RunComplete` (or "survived crash" bonus).

## 5) Distance score + money rewards

### Score source
Use world position along your uphill axis (or spline progress).

- `runDistance = maxDistanceReached - startDistance`
- UI updates continuously during ride.

### Reward formula (example)
- Base money: `floor(runDistance * 0.5)`
- Bonus for surviving ejection dodge: `+25%`
- Bonus tiers every distance milestone.

Track:
- `CurrentRunDistance`
- `BestDistance` (persistent)
- `Currency` (persistent)

Use `PlayerPrefs` for quick prototype persistence, then migrate to JSON save files if needed.

## 6) Upgrades and repairs system

Create an `UpgradeData` table (ScriptableObject or JSON) with levels/costs/effects.

### Suggested upgrade categories
- **Engine**: increases `movePower`
- **Brakes**: increases `brakePower`
- **Grip/Tires**: increases friction stiffness
- **Suspension**: better stability on rough terrain
- **Frame durability**: reduces ejection chance on moderate impact
- **Fuel tank (optional)**: longer runs

### Repair economy
After each run:
- Bike takes durability damage from impacts.
- If durability is low, performance is penalized until repaired.
- Player spends money to repair before next attempt.

This creates meaningful money decisions between progression (upgrades) and maintenance (repairs).

## 7) Obstacle and terrain generation

For replayability:
- Build modular obstacle prefabs.
- Spawn them from weighted pools by distance band.
- Include safe gaps so runs feel fair.

Distance bands example:
- 0-200m: easy spacing
- 200-500m: medium spacing + moving hazards
- 500m+: narrow paths, steep sections, chained hazards

## 8) UI screens you should add

- **HUD**: speed, distance, current money, bike durability
- **Crash UI**: dodge prompt and timer
- **Run summary**: distance, rewards, repairs cost
- **Garage**: upgrades, repair button, stat preview

## 9) Minimal script roadmap (clean and scalable)

Add these scripts (or equivalents):

- `GameManager` (state machine + flow)
- `RunManager` (distance and reward calculation)
- `BikeHealth` (durability/repair)
- `RiderFallController` (ragdoll + dodge controls)
- `BikeDangerCollision` (bike-hits-rider elimination check)
- `EconomyManager` (currency + purchase validation)
- `UpgradeManager` (apply purchased upgrades to bike stats)
- `SaveSystem` (best distance, money, owned upgrade levels)

## 10) Practical implementation order

1. Add game state machine + run reset flow.
2. Add distance tracking + run summary UI.
3. Add currency rewards and persistence.
4. Add garage upgrades that affect current bike values.
5. Add bike durability + repair costs.
6. Implement dodge phase and bike-danger elimination.
7. Tune terrain/obstacles and balancing values.

## 11) Balancing tips

- Early runs should feel winnable in < 60 seconds.
- Avoid instant unavoidable deaths from random obstacle spawns.
- Keep upgrade gains noticeable but not overpowering.
- Test failure reasons: if most deaths are from unfair tumbles, lower slope spikes and increase suspension forgiveness.

---

If you want, the next step can be a concrete script-by-script implementation plan based on your current scene hierarchy and prefab setup.
