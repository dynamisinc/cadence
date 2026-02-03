# Changelog

All notable changes to the Cadence API will be documented in this file.

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
