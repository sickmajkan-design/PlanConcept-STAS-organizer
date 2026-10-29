# Apple App Review: lokacija "uvijek" i privatnost (nacrt)

Za App Store Connect. Tekstovi su na engleskom jer ih čita Appleov pregled. Žuta polja u javnim stranicama
(`/privatnost.html`, `/podrska.html`) treba popuniti prije slanja. Pravni osnov za praćenje je i dalje otvoren
(`docs/PRIVACY.md` §6): bez odluke vlasnika i pravnika ne šaljemo aplikaciju na pregled.

## App Review notes (polje "Notes")
Construction Organizer is a B2B workforce app. Accounts are created by an employer; there is no public sign-up.
Demo account: <korisnik / lozinka / adresa servera>.

Background location: the employer's office sees which employees are on which construction site (live map and proof of
presence). The app sends the device position about once a minute **only while the employee is clocked in to a shift**,
including in the background; it starts at clock-in and stops at clock-out, and nothing is sent off shift. It is not used for advertising, analytics or sharing with third parties.
We use the "Always" permission because workers keep the phone in a pocket while working; "When In Use" would stop
reporting as soon as the screen locks. Employees are informed by their employer before accounts are issued.

## App Privacy ("nutrition label")
- Precise Location: collected, linked to the user, purpose App Functionality, not used for tracking.
- Name, Email, Phone: collected, linked to the user, App Functionality.
- Photos: collected (user-chosen attachments), linked, App Functionality.
- Device ID (push token): collected, linked, App Functionality.
- Tracking: **No**.

## Adrese
- Privacy Policy URL: `https://<domena>/privatnost.html`
- Support URL: `https://<domena>/podrska.html`
