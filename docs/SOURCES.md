# Source map

| Exact reference | Understood behavior | Original 3D implementation |
| --- | --- | --- |
| Unknown Horizons `content/scenarios/tutorial_en.yaml`, `events` at lines 355–514 on inspected master | Resource thresholds plus progress guards unlock connected production/building objectives | Original English camp/production/expedition goals inspect actual saved state; completed IDs prevent repeat reward |
| Unknown Horizons `horizons/scenario/actions.py` | Registered actions change logbook/state; resource grants are explicit actions | UI requests feed an independent simulation; rewards are part of original quest definitions |
| Cataclysm-DDA `doc/JSON/JSON_INFO.md` and project license | Explicit item units, data definitions and separated survival properties | Original masses, capacity, needs, food age and recipes in JSON; no copied CDDA numbers or content |
| Natural Earth `geojson/ne_10m_admin_0_countries.geojson`, commit `9380cca83db5f9aef52d5e762765100745f84b27` | Geographic outer coast polygons; source scale is cartographic generalization | Regional overview and Jamaica-local coast use the same clipped data; original procedural relief is labeled separately |

Reference code/content is **not included**. Scenario/event and CDDA references
are analysis sources, not a claim of compatible relicensing. No direct code or
scenario-text import occurred. A future direct import requires exact file version,
license review, notice and a documented adaptation record.

Primary links:
- https://github.com/unknown-horizons/unknown-horizons/blob/master/content/scenarios/tutorial_en.yaml
- https://github.com/unknown-horizons/unknown-horizons/blob/master/horizons/scenario/actions.py
- https://github.com/CleverRaven/Cataclysm-DDA/blob/master/doc/JSON/JSON_INFO.md
- https://www.naturalearthdata.com/about/terms-of-use/

Own balance is `first-playable-1`; it is not extracted from any reference game.
Local projection: x = R cos(latitude_origin) Δlongitude, z = −R Δlatitude,
angles in radians, R = 6371008.8 m. Overview uses a regional 18° reference latitude.
This is a documented regional approximation, not a globally distance-preserving
projection. Longitude degrees are not treated as latitude metres. Local scale
is approximately one engine unit per metre near the starting anchor.
