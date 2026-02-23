# Changelog

All notable changes to the Cadence API will be documented in this file.

## [3.8.0](https://github.com/dynamisinc/cadence/compare/api-v3.7.0...api-v3.8.0) (2026-02-23)


### Features

* **assignments:** show organization name on cards when user has multi-org assignments ([0c08379](https://github.com/dynamisinc/cadence/commit/0c08379b0ad7c8d21973e291e44f3c3b4a7d84d7))
* **exercises:** add detailed fields to ExerciseDto and update ExerciseTable for expandable rows ([e1a9a90](https://github.com/dynamisinc/cadence/commit/e1a9a901459890f6ba8fa0d2f3c4d4cfe4b8ab9c))

## [3.7.0](https://github.com/dynamisinc/cadence/compare/api-v3.6.0...api-v3.7.0) (2026-02-21)


### Features

* **autocomplete:** add block/unblock UI and historical values management ([e4eeaa1](https://github.com/dynamisinc/cadence/commit/e4eeaa1cdf22ffa8751ba5e226eb89c9b839027a))
* **autocomplete:** add suggestion blocklist for suppressing historical values ([77e723f](https://github.com/dynamisinc/cadence/commit/77e723f7dbb0680a0f9d014d053fbaff606efe14))
* **clock:** add max duration and manual time setting ([de6a434](https://github.com/dynamisinc/cadence/commit/de6a434499f90840359de6ed8282a81fa2ae5d04))
* **delivery-methods:** add SysAdmin management UI for delivery methods ([3799423](https://github.com/dynamisinc/cadence/commit/37994237d4b9388c0b332b005ee1c1d31e843bc2))
* UAT cleanup - clock, delivery methods, autocomplete, auth & photo fixes ([b08516a](https://github.com/dynamisinc/cadence/commit/b08516a12c1c1ba58862b06a1f1c5da5af86599e))


### Bug Fixes

* **auth:** escalate sys/org admin permissions above limited exercise roles ([e40b7f6](https://github.com/dynamisinc/cadence/commit/e40b7f69113dec1980147b0f1edce2e9c3e44927))
* **photos:** resolve blob URIs to SAS URLs before returning to clients ([f0347ae](https://github.com/dynamisinc/cadence/commit/f0347aefa435e16323f25441fc7c218b84d76acd))

## [3.6.0](https://github.com/dynamisinc/cadence/compare/api-v3.5.0...api-v3.6.0) (2026-02-13)


### Features

* add org-level autocomplete suggestion management and inject detail enhancements ([be774c2](https://github.com/dynamisinc/cadence/commit/be774c27576db39a54f9c8e050359336351ccfed))


### Bug Fixes

* **api:** add missing fields to ExerciseParticipantDto ([f02e8bc](https://github.com/dynamisinc/cadence/commit/f02e8bce1fd7d0613efff863fa295edc1767f016))
* **api:** align MselSummaryDto property names with InjectStatus enum ([88e70e6](https://github.com/dynamisinc/cadence/commit/88e70e691d07847d66f45a50d8c4eaf19850b9cb))
* renumber injects on MSEL reorder so # stays ascending ([ecb3a02](https://github.com/dynamisinc/cadence/commit/ecb3a02eb0053b8821fd3e0413f18d4f63e5c1c1))

## [3.5.0](https://github.com/dynamisinc/cadence/compare/api-v3.4.1...api-v3.5.0) (2026-02-11)


### Features

* **photos:** add offline photo queue and annotation editor (S05, S06) ([4386f91](https://github.com/dynamisinc/cadence/commit/4386f91124e14ad239461735aa751e146e2cdd77))
* **photos:** offline photo queue and annotation editor (S05, S06) ([0440cad](https://github.com/dynamisinc/cadence/commit/0440cad0af6e9fdf70858a73d6171513f3badfef))


### Bug Fixes

* **database:** resolve startup crash from duplicate index and query filter warnings ([503223c](https://github.com/dynamisinc/cadence/commit/503223c9e0f7d948f28b497f426aa6fba904aac6))

## [3.4.1](https://github.com/dynamisinc/cadence/compare/api-v3.4.0...api-v3.4.1) (2026-02-11)


### Bug Fixes

* **invitation:** show org name, exercises on invite page and fix registration UX ([e805b99](https://github.com/dynamisinc/cadence/commit/e805b99a8f052dd045c5249353bd6645de99a3fe))
* **invitations:** show org name, exercises, and fix registration UX ([252ee2f](https://github.com/dynamisinc/cadence/commit/252ee2fc0794ee8393623458a06f24dcb6094b36))

## [3.4.0](https://github.com/dynamisinc/cadence/compare/api-v3.3.0...api-v3.4.0) (2026-02-11)


### Features

* **field-operations:** add photo capture, gallery, and observation enhancements ([2e691a0](https://github.com/dynamisinc/cadence/commit/2e691a01108fb594a346d5b03c282d0d69f1d0b8))
* **photos:** add photo capture, gallery, deletion, and recycle bin ([05b7a20](https://github.com/dynamisinc/cadence/commit/05b7a2034c4cf67df2ba53ec123e0f1045960b6d))


### Bug Fixes

* **photos:** address code review findings for photo feature ([04a51e7](https://github.com/dynamisinc/cadence/commit/04a51e7bc4e542efc30b0a3446b44f70f441f0f9))


### Performance Improvements

* **observations:** optimize queries with projection and composite indexes ([6f3e474](https://github.com/dynamisinc/cadence/commit/6f3e474b183bf97b530d4dea9fe8c790eb3b711b))

## [3.3.0](https://github.com/dynamisinc/cadence/compare/api-v3.2.0...api-v3.3.0) (2026-02-10)


### Features

* **bulk-participant-import:** add pending invitations section ([22b5531](https://github.com/dynamisinc/cadence/commit/22b5531eb05444150d12439bfc2837c48188bc5c))
* **bulk-participant-import:** implement bulk participant import with drag-drop and pending invitations ([653a2bb](https://github.com/dynamisinc/cadence/commit/653a2bb58708b609e748cfce3affd7e066508f0b))
* **invitation:** add exercise-aware invitation emails ([52a8685](https://github.com/dynamisinc/cadence/commit/52a86859553bda6abd5fa07546876882650f5736))


### Bug Fixes

* **backend:** resolve nullable reference warnings in services ([184760a](https://github.com/dynamisinc/cadence/commit/184760a992f8b20b7a85a69166fcf50da6e45f2a))
* **bulk-participant-import:** integrate email delivery for invitations ([04e5e35](https://github.com/dynamisinc/cadence/commit/04e5e358dff673b2874ae2ecdd0599ced3550a8f))

## [3.2.0](https://github.com/dynamisinc/cadence/compare/api-v3.1.0...api-v3.2.0) (2026-02-09)


### Features

* **email:** add authentication email templates and integration (Phase 2) ([e897614](https://github.com/dynamisinc/cadence/commit/e8976143b748074953fe045dd17d23ba84f0a5ab))
* **email:** add email communications system with templates, invitations, and preferences ([d3ac229](https://github.com/dynamisinc/cadence/commit/d3ac229814f591acec2b594dffe941404a460216))
* **email:** add email infrastructure foundation (Phase 1) ([a071264](https://github.com/dynamisinc/cadence/commit/a071264276a87128bd7be598c6ac1084532215cf))
* **email:** add email preferences UI, feedback forms, and error reporting ([a95095e](https://github.com/dynamisinc/cadence/commit/a95095e69fdda2c4bab72574ced389606fbb842a))
* **email:** add exercise invitation email templates (Phase 4) ([9a44dd9](https://github.com/dynamisinc/cadence/commit/9a44dd9f4900d846dc3443e578e402f758e46dcc))
* **email:** add invitation UI with email delivery and accept flow ([181a3d3](https://github.com/dynamisinc/cadence/commit/181a3d3130e3e395f4d23ca7bfc7a6dfb0a8b6a0))
* **email:** add organization invitation system (Phase 3) ([ec91c1d](https://github.com/dynamisinc/cadence/commit/ec91c1df8d54026c3b78670a27b2c9f6915fcef1))
* **email:** add status, support, reminder, and digest templates (EM-07 through EM-10) ([eed266f](https://github.com/dynamisinc/cadence/commit/eed266f609057b571a1af5ed24ba751e9d3dcf92))
* **email:** add structured production logging and EF migration ([15e764a](https://github.com/dynamisinc/cadence/commit/15e764a5f39c472437f290e0d42a1f6739a0d2b6))
* **email:** add workflow and assignment notification templates (EM-05, EM-06) ([87aff49](https://github.com/dynamisinc/cadence/commit/87aff49150005cf43ec550d373fe1d5bb268b2c3))
* **email:** integrate Azure Communication Services for email delivery ([bb15c57](https://github.com/dynamisinc/cadence/commit/bb15c57f45a49dd9a35f615e1f8fe7b62d81e148))
* **invitation:** smart invite flow with account detection and return URLs ([4a9da41](https://github.com/dynamisinc/cadence/commit/4a9da41945bbd67a810c7a2bb5a63ede8b6c6711))
* **system-settings:** add admin UI for email configuration overrides ([7f42341](https://github.com/dynamisinc/cadence/commit/7f4234171c9b690db1ca3eb856b0378dfad5c79c))


### Bug Fixes

* **auth:** resolve DbContext concurrency in registration and password reset ([0803f07](https://github.com/dynamisinc/cadence/commit/0803f07009bfb2c9bd55592ca8aa502aa1e19a24))
* **email:** address code review findings across auth and invitations ([f732352](https://github.com/dynamisinc/cadence/commit/f732352fa8a2f75276d8ed87fc5a3c04da306c73))
* **email:** block reserved domains and force Logging provider in tests ([2706ecc](https://github.com/dynamisinc/cadence/commit/2706ecc0d15bbb22447bd4d47ca39987b8b61b3c))
* **email:** remove double-retry and restore App Insights logging provider ([8d7d542](https://github.com/dynamisinc/cadence/commit/8d7d542ad84df5f2b28d3d2300c5305a5bdb964a))
* **invitation:** add organizationName to InvitationDto and frontend type ([028491a](https://github.com/dynamisinc/cadence/commit/028491a063821c30c22a9885564dadc29d2df280))
* **invitation:** bypass org query filter and fix invite display fields ([ced1c25](https://github.com/dynamisinc/cadence/commit/ced1c25b37a33ecec7671ded2c46dc8f0399a8b9))
* **invitation:** complete new-user invitation flow with auto-accept and org context ([480e5e8](https://github.com/dynamisinc/cadence/commit/480e5e815caf39a11d601db0b53bfec32227c46c))

## [3.1.0](https://github.com/dynamisinc/cadence/compare/api-v3.0.0...api-v3.1.0) (2026-02-06)


### Features

* **eeg:** add Exercise Evaluation Guide feature ([d7365d4](https://github.com/dynamisinc/cadence/commit/d7365d4182dc9e7ed5f8ceebed95de4099ee3df8))
* **eeg:** add grouped views, SignalR updates, DnD reorder, inject linking ([cbbe413](https://github.com/dynamisinc/cadence/commit/cbbe413a18429c9d9a09d24571a5d3d4782c6e4d))
* **eeg:** Exercise Evaluation Guide (EEG) feature ([42a4369](https://github.com/dynamisinc/cadence/commit/42a4369b66528d2bb1367a820c182d29d342183b))
* **eeg:** implement EEG document generation (S13a/S13b) ([d3670d0](https://github.com/dynamisinc/cadence/commit/d3670d0ca295c921d02e425869983ada173bb14b))


### Bug Fixes

* **eeg:** stabilize dialog size and add EEG entry to observations page ([de16fc7](https://github.com/dynamisinc/cadence/commit/de16fc70400d27fe6ff0e0963d2935fffdac7efb))

## [3.0.0](https://github.com/dynamisinc/cadence/compare/api-v2.0.0...api-v3.0.0) (2026-02-04)


### ⚠ BREAKING CHANGES

* **inject-approval:** InjectStatus enum values renamed to HSEEP terminology

### Features

* **inject-approval:** add approval workflow entity fields and HSEEP terminology ([01c3aff](https://github.com/dynamisinc/cadence/commit/01c3aff195a8bd16e1294671b44d6bef5abdab18))
* **inject-approval:** enforce self-approval policy and edit invalidation (S11, S15) ([a862369](https://github.com/dynamisinc/cadence/commit/a862369ec2184a79ab0d1d1758c65f53bcf37d6b))
* **inject-approval:** Implement approval workflow with configurable permissions ([d8861f4](https://github.com/dynamisinc/cadence/commit/d8861f4922e0ae2779f95ede36f893edd8686fee))
* **inject-approval:** implement S00 HSEEP-compliant InjectStatus enum ([43ca1cd](https://github.com/dynamisinc/cadence/commit/43ca1cd77f432a8c04b0b5cb22cd50d07492dcb2))
* **inject-approval:** implement S01-S04 approval workflow services and endpoints ([25908b0](https://github.com/dynamisinc/cadence/commit/25908b0e17877824a874cf7fe8f8b51bb81cc7da))
* **inject-approval:** implement S05-S07 batch approval, queue view, go-live gate ([7ade1c1](https://github.com/dynamisinc/cadence/commit/7ade1c1716065645305ad43194369bc557d4ff84))
* **inject-approval:** implement S08-S09 notifications and revert approval ([2a377dc](https://github.com/dynamisinc/cadence/commit/2a377dc2aef09fe6d5cf1f47a924f2cf8e8c8eb5))
* **inject-approval:** implement S11 configurable approval permissions and frontend components ([aae6ad0](https://github.com/dynamisinc/cadence/commit/aae6ad0bce5207a336744fdf430af142ca608e71))


### Bug Fixes

* **inject-approval:** resolve permissions persistence and self-approval issues ([50663e2](https://github.com/dynamisinc/cadence/commit/50663e2f8b295293005a6d5e87a1fbf6e673b382))
* **inject-approval:** resolve permissions persistence and self-approval issues ([462a5c7](https://github.com/dynamisinc/cadence/commit/462a5c7b4bf2ffe5b5d00af035478043b41ca902))
* **migrations:** resolve inject status and FK cascade issues ([5afe1e5](https://github.com/dynamisinc/cadence/commit/5afe1e5f09e91a0b1b874a6536694de7f317cd54))

## [2.0.0](https://github.com/dynamisinc/cadence/compare/api-v1.1.0...api-v2.0.0) (2026-02-03)


### ⚠ BREAKING CHANGES

* **db:** All audit fields now use string type instead of Guid

### Features

* **frontend:** add status and organization filters to user management ([175ad58](https://github.com/dynamisinc/cadence/commit/175ad58f7c27faaad0e585140802f4dbb840cb83))


### Bug Fixes

* **backend:** allow admins to set SystemRole when creating users ([6eb46a7](https://github.com/dynamisinc/cadence/commit/6eb46a7f3106990ad61e9fbb25b7a81bfbe7e8ae))


### Miscellaneous Chores

* **db:** deprecate legacy User table and standardize audit columns ([2c44dc4](https://github.com/dynamisinc/cadence/commit/2c44dc4f3e1571759181de90d036e89a7bfafad3))

## [1.1.0](https://github.com/dynamisinc/cadence/compare/api-v1.0.0...api-v1.1.0) (2026-02-01)


### Features

* add clock-based inject workflow with time-based sections ([aec36bf](https://github.com/dynamisinc/cadence/commit/aec36bf729d65e68836545b998bc3ab5061e4cd3))
* add core domain entities per HSEEP specification ([d3d3a18](https://github.com/dynamisinc/cadence/commit/d3d3a188e36a650a8ea3fe281fcd3b0212396b71))
* Add Exercise Capabilities, Metrics, and Settings features ([c45d565](https://github.com/dynamisinc/cadence/commit/c45d565764f9a414d1043d038d953e958aa4d82c))
* add exercise conduct and observations (Phase D+E) ([d2aae66](https://github.com/dynamisinc/cadence/commit/d2aae66b87ffc749ba6938c6fc3fd3e6dfe46598))
* add HomePage with role-aware dashboard ([993f13a](https://github.com/dynamisinc/cadence/commit/993f13a6ba46f672a552ee7d5e8093ddf10d6aaa))
* add inject reorder API endpoint with SignalR notification ([9af8a27](https://github.com/dynamisinc/cadence/commit/9af8a27a12bdf5415d9a2f4f4f35eaa75573d6db))
* add objectives CRUD, inject-objective linking, timezone expansion, and practice mode ([7898b77](https://github.com/dynamisinc/cadence/commit/7898b77a6bdf63017cda3e600ba5d2787133fb0b))
* add Phase M.2 - exercise status workflow, MSEL management, and duplication ([6cb1f12](https://github.com/dynamisinc/cadence/commit/6cb1f12740d4a05ed269474254a096bdf496ecc2))
* add real-time sync and offline capability (Phase H) ([0f08b08](https://github.com/dynamisinc/cadence/commit/0f08b0814b8046f4482728f0d876844a6d8306c6))
* **capabilities:** add reactivation and fix import refresh ([e6ea549](https://github.com/dynamisinc/cadence/commit/e6ea54982f2d728de208ad0ebde5a33410015b39))
* **capabilities:** implement exercise capabilities Phase 2 (S03, S04, S05, S06) ([4a4e31d](https://github.com/dynamisinc/cadence/commit/4a4e31d0d89f410f2eb99a3efda55a124697046e))
* **capabilities:** refactor CoreCapability to organization-scoped Capability entity (S01) ([8419a6f](https://github.com/dynamisinc/cadence/commit/8419a6f39a239c885fa57075cc406c5cf2c8c148))
* Complete Authentication & Authorization System (S01-S15, S25) ([bacc65a](https://github.com/dynamisinc/cadence/commit/bacc65a95f3bb1a008d26f1b77df834d593df32e))
* complete Excel export integration with enhanced template ([a77c56a](https://github.com/dynamisinc/cadence/commit/a77c56a8f0c9a4194d83d918dbe8793d59a95492))
* Complete Excel export integration with enhanced template ([fc9afca](https://github.com/dynamisinc/cadence/commit/fc9afcaee8fc0587c32a6b1f33401ab65057765a))
* complete Exercise CRUD feature with full-stack implementation ([4245936](https://github.com/dynamisinc/cadence/commit/4245936f2d80696cd9f15ab9e65902c771775833))
* Conduct Page UX Improvements ([408591f](https://github.com/dynamisinc/cadence/commit/408591f89873b5ae3604e577251bf7029e1bb31f))
* Exercise Clock Modes and MSEL Drag-Drop Reordering ([cdb3aaa](https://github.com/dynamisinc/cadence/commit/cdb3aaadc0ec60d0bb6c59c550863fe9834e7ca9))
* exercise conduct with clock-based inject workflow ([b4063ea](https://github.com/dynamisinc/cadence/commit/b4063eaf88e8d44623ead6fdb9935c5c78314ab1))
* Exercise Observations enhancements and UX improvements ([bd8388d](https://github.com/dynamisinc/cadence/commit/bd8388daa5f937da1e8931eb111224500dc1d5c2))
* **exercises:** consolidate exercise lists into shared ExerciseTable component ([a3a8caf](https://github.com/dynamisinc/cadence/commit/a3a8caf922704c920f8884aae742daa0cd9554a7))
* implement authentication and authorization system ([489ad03](https://github.com/dynamisinc/cadence/commit/489ad0398640c1e55cd61c8d10f0b57026bae68d))
* implement Exercise Clock Modes feature (CLK-01 through CLK-10) ([a77af4f](https://github.com/dynamisinc/cadence/commit/a77af4f974b77a43bef6afdc22e8365d12e76dd1))
* implement exercise participant management and role resolution UI (S14-S15) ([d855f12](https://github.com/dynamisinc/cadence/commit/d855f12be74f96afcdd6eaff83b0a52b061180a6))
* implement Inject CRUD and Phase Management with comprehensive tests ([9d6a252](https://github.com/dynamisinc/cadence/commit/9d6a252832f9c4c9dac9759727332622579a225d))
* implement inline user creation from exercise participants (S25) ([d78b8bc](https://github.com/dynamisinc/cadence/commit/d78b8bc3b6219d2d0583bc0ca7ed7e9eb3452514))
* implement My Assignments and Notifications features (P0-03, P0-04) ([b720364](https://github.com/dynamisinc/cadence/commit/b72036419a474b46d677fca16aba05ba315a97ea))
* implement Reports & Export feature (P0-05) ([f5651e4](https://github.com/dynamisinc/cadence/commit/f5651e466a61c09df764b19de0f6fe067d0e9487))
* implement sidebar navigation, assignments, notifications, and reports (P0-03, P0-04, P0-05) ([3b464d5](https://github.com/dynamisinc/cadence/commit/3b464d5c186e87daa9c25039c504715b3068d192))
* MSEL Import from Excel with streamlined exercise creation ([abe9cb4](https://github.com/dynamisinc/cadence/commit/abe9cb44ff0aeb575a2b6ab57890c6ea0f194e29))
* MSEL import from Excel with streamlined exercise creation flow ([6390e71](https://github.com/dynamisinc/cadence/commit/6390e71426227a72be0e434aae0dd9b7d28b9927))
* **observations:** add capability tagging to conduct page ([61b6cb0](https://github.com/dynamisinc/cadence/commit/61b6cb0841888862354e5c80f4f6a9cd4bdec7e3))
* Organization Management with Multi-Tenant Data Isolation ([186bc14](https://github.com/dynamisinc/cadence/commit/186bc1459d7248aa1671ea0194f9a782742b0357))
* **organizations:** add member management and integration tests ([3277db7](https://github.com/dynamisinc/cadence/commit/3277db7ae71dac707a7f9a9458b1a63a7f259c3b))
* **organizations:** implement data isolation for multi-tenant security ([9cc7dc6](https://github.com/dynamisinc/cadence/commit/9cc7dc607b57312faeca4c42d9363952550b93d8))
* Phase G (Inject Organization) + Phase H (Real-Time Sync & Offline) ([46f2159](https://github.com/dynamisinc/cadence/commit/46f2159bee597a2d0dbc9ac7cc3d62780f5a76a3))
* Phase M - Objectives, timezone expansion, and practice mode ([714a51a](https://github.com/dynamisinc/cadence/commit/714a51abeb8c3c68f692ae0177f6b4cacbc12437))
* Phase M.2 - Exercise Status Workflow, MSEL Management & Optimistic Updates ([aaecfd7](https://github.com/dynamisinc/cadence/commit/aaecfd75cc4e1b7e93185b6faac1d825dcf136d1))
* **seeding:** add demo data with environment-specific config ([049a823](https://github.com/dynamisinc/cadence/commit/049a8233274856bded73b3c05bcbae5d2f68b50f))
* **seeding:** add EssentialDataSeeder for production-safe initialization ([345c9bb](https://github.com/dynamisinc/cadence/commit/345c9bbd1fa9b62b59e935a30192a58bab1c8f86))
* **settings-metrics:** add settings/metrics feature specs and confirmation dialogs ([b3a97f2](https://github.com/dynamisinc/cadence/commit/b3a97f2e9e48d23f275bd1c655fb044953663ad9))
* **settings:** wire confirmation dialogs to exercise settings ([00e672c](https://github.com/dynamisinc/cadence/commit/00e672cc4c1b4460afdf779c54f28c54df76c2bb))
* **ui:** add versioning and release notes feature ([262f884](https://github.com/dynamisinc/cadence/commit/262f8849706dd3171269edd8429b62912bba93b4))


### Bug Fixes

* add defensive bounds checks for Excel import ([3e0ca82](https://github.com/dynamisinc/cadence/commit/3e0ca821d0cb21e5d8f578df68750c36509a0cb9))
* add HasContext to bypass org validation during seeding ([2ad6025](https://github.com/dynamisinc/cadence/commit/2ad6025376fd479ec2f9d55113741fddb3bf0a7a))
* add inject number to observations and implement edit functionality ([64bac9a](https://github.com/dynamisinc/cadence/commit/64bac9a0a9eb3a9bb281d2d154fae907e74b3f1d))
* add SignalR notifications to InjectsController and InjectStatusChanged event ([3a0f3dd](https://github.com/dynamisinc/cadence/commit/3a0f3dd80236d3bd5bcff1f4813dc0a5d896a3e9))
* address code review findings from PR [#14](https://github.com/dynamisinc/cadence/issues/14) ([baa967f](https://github.com/dynamisinc/cadence/commit/baa967f3a5dba66f828e0c9a9fc138414e2165e4))
* address code review issues for organization management ([fde84f9](https://github.com/dynamisinc/cadence/commit/fde84f9dba3a7518df226151c2a8eaec83c7784b))
* address code review issues for PR [#13](https://github.com/dynamisinc/cadence/issues/13) ([fe8ea9c](https://github.com/dynamisinc/cadence/commit/fe8ea9ca7adf58511556ff701fcf3496a0bd5ebf))
* address code review issues from PR 15 ([2e7fc6a](https://github.com/dynamisinc/cadence/commit/2e7fc6a46dd1354d8b0a563565a6d1fb3d9801d0))
* **api:** resolve compiler warnings in backend and frontend ([2c8b7d8](https://github.com/dynamisinc/cadence/commit/2c8b7d82bf03d8f8859c05325d8afb5b1abe151a))
* **auth:** correct Remember Me session handling and cookie expiration ([8024256](https://github.com/dynamisinc/cadence/commit/8024256138c8a9f462bd0b5e2072273c8cee5ada))
* **build:** disable default embedded resource items ([7b8a703](https://github.com/dynamisinc/cadence/commit/7b8a7030a3af01b6546709bdb30f086c1a066eed))
* CLAUDE.md compliance and eslint errors ([1bfb645](https://github.com/dynamisinc/cadence/commit/1bfb64540a318116d69991b84d1e32be5772b76d))
* **clock:** store ClockElapsedBeforePause as bigint to support &gt;24h durations ([81ab693](https://github.com/dynamisinc/cadence/commit/81ab693d871bf6a406d017d3db9c7f118b10b786))
* **clock:** store ClockEvent.ElapsedTimeAtEvent as bigint ([db01af7](https://github.com/dynamisinc/cadence/commit/db01af7c4ffd90da0f2609c981768399dc1567bd))
* **exercises:** improve Exercise Director dropdown and offline handling ([4bb5ad7](https://github.com/dynamisinc/cadence/commit/4bb5ad71b706b00f7b06e7912503af541ccdefda))
* **frontend:** resolve ESLint errors and .NET formatting issues ([58102ce](https://github.com/dynamisinc/cadence/commit/58102cec14f7d316af87cb11a50dab4230b679ff))
* improve content-disposition parsing and add deprecation warning ([c5d4f94](https://github.com/dynamisinc/cadence/commit/c5d4f946d1ea79dafd539a03395909818c955535))
* improve thread safety and document session storage limitations ([102b070](https://github.com/dynamisinc/cadence/commit/102b07090d75685dc87efef693e933daf3193024))
* observation FK constraint and reports page UX ([1f217f4](https://github.com/dynamisinc/cadence/commit/1f217f4782926a6b9c58029c90eda1cb9991e1ae))
* **organizations:** complete organization switching and context display ([782d1b4](https://github.com/dynamisinc/cadence/commit/782d1b456b54d907cdd51520aca87a874d197f64))
* resolve exercise participants and details page issues ([5e0f1ba](https://github.com/dynamisinc/cadence/commit/5e0f1ba62d96bd4177032b0508ecace4bd2484d4))
* resolve TypeScript build errors in test fixtures and DTOs ([80a9c5f](https://github.com/dynamisinc/cadence/commit/80a9c5fa1097c1764f0c25a795327900bb7de632))
* **seeding:** add unique Slug values to organization seeders ([cc78438](https://github.com/dynamisinc/cadence/commit/cc78438ea8f3f7144ed1d20e8c60c5b652b6e86a))


### Performance Improvements

* **capabilities:** optimize query performance ([7b5199c](https://github.com/dynamisinc/cadence/commit/7b5199c4830477aae16016e4749fd47bc305de6b))

## [1.0.0] - 2026-01-30

### Features

* Exercise CRUD operations with full lifecycle management
* Inject/MSEL management with phase organization
* Real-time synchronization via SignalR
* Offline capability with IndexedDB caching and sync queue
* Observation capture for evaluators
* Multi-user exercise participation

### Technical

* .NET 10 backend with Entity Framework Core
* SQLite for local development, SQL Server for production
* JWT authentication with refresh tokens
