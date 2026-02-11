# Changelog

All notable changes to Cadence will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.5.1](https://github.com/dynamisinc/cadence/compare/frontend-v2.5.0...frontend-v2.5.1) (2026-02-11)


### Bug Fixes

* **invitation:** show org name, exercises on invite page and fix registration UX ([e805b99](https://github.com/dynamisinc/cadence/commit/e805b99a8f052dd045c5249353bd6645de99a3fe))
* **invitations:** show org name, exercises, and fix registration UX ([252ee2f](https://github.com/dynamisinc/cadence/commit/252ee2fc0794ee8393623458a06f24dcb6094b36))

## [2.5.0](https://github.com/dynamisinc/cadence/compare/frontend-v2.4.0...frontend-v2.5.0) (2026-02-11)


### Features

* **field-operations:** add photo capture, gallery, and observation enhancements ([2e691a0](https://github.com/dynamisinc/cadence/commit/2e691a01108fb594a346d5b03c282d0d69f1d0b8))
* **photos:** add inline photo gallery to observation list ([5303730](https://github.com/dynamisinc/cadence/commit/53037303bb18d9940b56c27f6c3f27ad67677b89))
* **photos:** add photo capture, gallery, deletion, and recycle bin ([05b7a20](https://github.com/dynamisinc/cadence/commit/05b7a2034c4cf67df2ba53ec123e0f1045960b6d))
* **photos:** show observation details on linked photo gallery items ([231fffb](https://github.com/dynamisinc/cadence/commit/231fffb437cc9aa2b1fd253681d4248d7d9d0e81))


### Bug Fixes

* **photos:** address code review findings for photo feature ([04a51e7](https://github.com/dynamisinc/cadence/commit/04a51e7bc4e542efc30b0a3446b44f70f441f0f9))
* **photos:** resolve CI lint, type, and test errors for photo feature ([8a123bd](https://github.com/dynamisinc/cadence/commit/8a123bd29aefb522398f3371884ca4b046cc86dd))
* **photos:** stage photos locally and upload only on form submit ([454ae7e](https://github.com/dynamisinc/cadence/commit/454ae7e74d130c47db86170889cc794dc2071ca1))

## [2.4.0](https://github.com/dynamisinc/cadence/compare/frontend-v2.3.0...frontend-v2.4.0) (2026-02-10)


### Features

* **bulk-participant-import:** implement bulk participant import with drag-drop and pending invitations ([653a2bb](https://github.com/dynamisinc/cadence/commit/653a2bb58708b609e748cfce3affd7e066508f0b))

## [2.3.0](https://github.com/dynamisinc/cadence/compare/frontend-v2.2.0...frontend-v2.3.0) (2026-02-09)


### Features

* **logging:** implement Serilog structured logging ([2884710](https://github.com/dynamisinc/cadence/commit/2884710e8930d1f00f738af6f77d409fc9c0cd8e))


### Bug Fixes

* **pwa:** persist install banner dismiss with 90-day cooldown ([7355324](https://github.com/dynamisinc/cadence/commit/73553242f1799ca0569c85a2908d09ebd384a507))

## [2.2.0](https://github.com/dynamisinc/cadence/compare/frontend-v2.1.0...frontend-v2.2.0) (2026-02-09)


### Features

* **email:** add email communications system with templates, invitations, and preferences ([d3ac229](https://github.com/dynamisinc/cadence/commit/d3ac229814f591acec2b594dffe941404a460216))
* **email:** add email preferences UI, feedback forms, and error reporting ([a95095e](https://github.com/dynamisinc/cadence/commit/a95095e69fdda2c4bab72574ced389606fbb842a))
* **email:** add exercise invitation dialog and participant list updates ([2993ecf](https://github.com/dynamisinc/cadence/commit/2993ecf59e96116f986c34eda547293d61e144bc))
* **email:** add invitation UI with email delivery and accept flow ([181a3d3](https://github.com/dynamisinc/cadence/commit/181a3d3130e3e395f4d23ca7bfc7a6dfb0a8b6a0))
* **invitation:** smart invite flow with account detection and return URLs ([4a9da41](https://github.com/dynamisinc/cadence/commit/4a9da41945bbd67a810c7a2bb5a63ede8b6c6711))
* **system-settings:** add admin UI for email configuration overrides ([7f42341](https://github.com/dynamisinc/cadence/commit/7f4234171c9b690db1ca3eb856b0378dfad5c79c))


### Bug Fixes

* **auth:** show error on invalid login and improve password field UX ([8579647](https://github.com/dynamisinc/cadence/commit/857964734e52c843e7eecb07f202855df9218ae6))
* **email:** address code review findings across auth and invitations ([f732352](https://github.com/dynamisinc/cadence/commit/f732352fa8a2f75276d8ed87fc5a3c04da306c73))
* **invitation:** add organizationName to InvitationDto and frontend type ([028491a](https://github.com/dynamisinc/cadence/commit/028491a063821c30c22a9885564dadc29d2df280))
* **invitation:** bypass org query filter and fix invite display fields ([ced1c25](https://github.com/dynamisinc/cadence/commit/ced1c25b37a33ecec7671ded2c46dc8f0399a8b9))
* **invitation:** complete new-user invitation flow with auto-accept and org context ([480e5e8](https://github.com/dynamisinc/cadence/commit/480e5e815caf39a11d601db0b53bfec32227c46c))
* **tests:** update test assertions for component changes ([197b1c7](https://github.com/dynamisinc/cadence/commit/197b1c7aebf79989e378cb3f4226c47e9e7b6a60))
* **ui:** resolve lint errors and failing frontend tests ([4bfa34c](https://github.com/dynamisinc/cadence/commit/4bfa34c0bb028a7cc65ebe7d339824a91d6a50b3))

## [2.1.0](https://github.com/dynamisinc/cadence/compare/frontend-v2.0.0...frontend-v2.1.0) (2026-02-06)


### Features

* **eeg:** add EEG Entries page and fix authorization routes ([2a9abd3](https://github.com/dynamisinc/cadence/commit/2a9abd336429b48056abb143a6a7028431b36f55))
* **eeg:** add Exercise Evaluation Guide feature ([d7365d4](https://github.com/dynamisinc/cadence/commit/d7365d4182dc9e7ed5f8ceebed95de4099ee3df8))
* **eeg:** add grouped views, SignalR updates, DnD reorder, inject linking ([cbbe413](https://github.com/dynamisinc/cadence/commit/cbbe413a18429c9d9a09d24571a5d3d4782c6e4d))
* **eeg:** add tests and refactor evaluator contact prompt ([c681ce0](https://github.com/dynamisinc/cadence/commit/c681ce01989134412ab10219d3dcf8b413224e2b))
* **eeg:** Exercise Evaluation Guide (EEG) feature ([42a4369](https://github.com/dynamisinc/cadence/commit/42a4369b66528d2bb1367a820c182d29d342183b))
* **eeg:** implement EEG document generation (S13a/S13b) ([d3670d0](https://github.com/dynamisinc/cadence/commit/d3670d0ca295c921d02e425869983ada173bb14b))


### Bug Fixes

* **auth:** improve password field UX and icon sizing ([594664e](https://github.com/dynamisinc/cadence/commit/594664e57245c513eb0cbcb3de9f252f625246d4))
* **eeg:** add observed-at time field and fix UTC display ([3165ba5](https://github.com/dynamisinc/cadence/commit/3165ba5789e5bf23445ea34bbfe2d9af3e42a335))
* **eeg:** add org-scoping and authorization for capability targets ([4502a65](https://github.com/dynamisinc/cadence/commit/4502a65bc01ce71f2d02d93f88c47469a1673c82))
* **eeg:** align EegDocumentDialog test mocks with EegCoverageDto type ([013eb9a](https://github.com/dynamisinc/cadence/commit/013eb9a2d1b547e881457d5002d13306ec037f4a))
* **eeg:** compact critical task item layout on tablet/laptop screens ([c4d2a0f](https://github.com/dynamisinc/cadence/commit/c4d2a0f8e461a1d47fa86434c0683c01fab3d614))
* **eeg:** correct permission checks and edited timestamp detection ([b0486b4](https://github.com/dynamisinc/cadence/commit/b0486b4c2be42da6b5e337556287d5df28651a7a))
* **eeg:** fix 5 failing grouped view tests with theme provider and scoped queries ([e21b475](https://github.com/dynamisinc/cadence/commit/e21b475a8f195815b3e1f994d97e28e2ce6d2705))
* **eeg:** improve date formatting and add currentUserId for edit checks ([c59d202](https://github.com/dynamisinc/cadence/commit/c59d20235745bd77b32ee0ea62f01ddd88d58aa5))
* **eeg:** improve test stability and accessibility ([76c070d](https://github.com/dynamisinc/cadence/commit/76c070d2183730036c3dd82f37370f78e71309c9))
* **eeg:** stabilize dialog size and add EEG entry to observations page ([de16fc7](https://github.com/dynamisinc/cadence/commit/de16fc70400d27fe6ff0e0963d2935fffdac7efb))
* **eeg:** update coverage display in EegDocumentDialog and clean up exports in index ([619d0c3](https://github.com/dynamisinc/cadence/commit/619d0c3ba10f576f0aa73bbf031ce71094acba58))
* **eeg:** use correct loading property from useInjects hook ([897ff19](https://github.com/dynamisinc/cadence/commit/897ff1960f131af9468ceef27949105c80275e0d))
* **ui:** add route error fallback, fix password test blur events, wire up grouped EEG views ([bf1ce0c](https://github.com/dynamisinc/cadence/commit/bf1ce0c8f2efee767b04c99d67118de939b3a479))
* **ui:** resolve all ESLint errors and warnings across 17 files ([338314a](https://github.com/dynamisinc/cadence/commit/338314af465c8cbdea4d7699d8c8be0e4c93786d))

## [2.0.0](https://github.com/dynamisinc/cadence/compare/frontend-v1.2.0...frontend-v2.0.0) (2026-02-04)


### ⚠ BREAKING CHANGES

* **inject-approval:** InjectStatus enum values renamed to HSEEP terminology

### Features

* **core:** enhance ErrorBoundary with responsive design and user-friendly UI ([5c8a25f](https://github.com/dynamisinc/cadence/commit/5c8a25fb60e3d4adc96ccdc3f54f033a87352261))
* **feature-flags:** add route protection with FeatureFlagGuard component ([80da4ce](https://github.com/dynamisinc/cadence/commit/80da4ce0def5aa4cb3c4601ad14d8595b9d5637f))
* **inject-approval:** add approval workflow entity fields and HSEEP terminology ([01c3aff](https://github.com/dynamisinc/cadence/commit/01c3aff195a8bd16e1294671b44d6bef5abdab18))
* **inject-approval:** enforce self-approval policy and edit invalidation (S11, S15) ([a862369](https://github.com/dynamisinc/cadence/commit/a862369ec2184a79ab0d1d1758c65f53bcf37d6b))
* **inject-approval:** Implement approval workflow with configurable permissions ([d8861f4](https://github.com/dynamisinc/cadence/commit/d8861f4922e0ae2779f95ede36f893edd8686fee))
* **inject-approval:** implement S00 HSEEP-compliant InjectStatus enum ([43ca1cd](https://github.com/dynamisinc/cadence/commit/43ca1cd77f432a8c04b0b5cb22cd50d07492dcb2))
* **inject-approval:** implement S11 configurable approval permissions and frontend components ([aae6ad0](https://github.com/dynamisinc/cadence/commit/aae6ad0bce5207a336744fdf430af142ca608e71))
* **inject-approval:** implement S12-S14 approval workflow enhancements ([7975f47](https://github.com/dynamisinc/cadence/commit/7975f4732b6fce439e91cca1c922dd37f8b2bb26))
* **organizations:** add Organization section to sidebar for OrgAdmin management ([82ac81d](https://github.com/dynamisinc/cadence/commit/82ac81d69b127ff77d5507b7f8a819fcdc1703b4))


### Bug Fixes

* **feature-flags:** add organization section to FeatureFlagsAdmin ([8ca03ff](https://github.com/dynamisinc/cadence/commit/8ca03ff5c67ac1647b56570a2a300e3d70d52fb5))
* **inject-approval:** navigate back to MSEL after submit for approval ([ae0e3e3](https://github.com/dynamisinc/cadence/commit/ae0e3e3451a1c3803cf57ab383d51f82457676d9))
* **inject-approval:** resolve lint errors and update menu tests ([00aba4b](https://github.com/dynamisinc/cadence/commit/00aba4bee3ab8d337e6c165a6de738b6b167c978))
* **inject-approval:** resolve permissions persistence and self-approval issues ([50663e2](https://github.com/dynamisinc/cadence/commit/50663e2f8b295293005a6d5e87a1fbf6e673b382))
* **inject-approval:** resolve permissions persistence and self-approval issues ([462a5c7](https://github.com/dynamisinc/cadence/commit/462a5c7b4bf2ffe5b5d00af035478043b41ca902))
* **inject-approval:** resolve TypeScript build errors ([300395e](https://github.com/dynamisinc/cadence/commit/300395e9114c5d5bc396d855419e97e7d631d3e2))
* resolve linting errors and improve approval permissions UX ([22b14c7](https://github.com/dynamisinc/cadence/commit/22b14c71ccaec5d9822f78392e6671de7c1c943d))
* **tests:** update ErrorBoundary tests for ThemeProvider and new UI text ([019a7c4](https://github.com/dynamisinc/cadence/commit/019a7c4583525f16f045c602262230e515ead77e))

## [1.2.0](https://github.com/dynamisinc/cadence/compare/frontend-v1.1.0...frontend-v1.2.0) (2026-02-03)


### Features

* **frontend:** add status and organization filters to user management ([175ad58](https://github.com/dynamisinc/cadence/commit/175ad58f7c27faaad0e585140802f4dbb840cb83))
* **frontend:** improve user management admin page UX ([fb0978d](https://github.com/dynamisinc/cadence/commit/fb0978d45de5d28531a0d90ac483f29774386d5f))
* **telemetry:** add Application Insights integration for API and frontend ([295f617](https://github.com/dynamisinc/cadence/commit/295f61770d27f30055d25f1781f4850f45b37c81))
* **version:** improve release notification UX and public about page ([e17807b](https://github.com/dynamisinc/cadence/commit/e17807bda10a35af1115ee30c367c51e5486b6f3))


### Bug Fixes

* **frontend:** fix refreshAccessToken circular dependency in AuthContext ([e48954e](https://github.com/dynamisinc/cadence/commit/e48954e6bffc62a078a925dc8a8520e80dcc9aba))
* **frontend:** remove unused _hasMoreChanges variable ([d0e3aef](https://github.com/dynamisinc/cadence/commit/d0e3aef8ff4c78d26a780529adc02b96339b9b6f))
* **frontend:** resolve ESLint linting errors ([e3d2148](https://github.com/dynamisinc/cadence/commit/e3d2148ba92babae52ce61d7c766f73f9ea93016))
* **frontend:** resolve ESLint linting errors and warnings ([5852166](https://github.com/dynamisinc/cadence/commit/5852166ee854b4251e095234f5f64ccb99251df7))
* **tests:** update useReleaseNotes test to not require features array ([6c3aaef](https://github.com/dynamisinc/cadence/commit/6c3aaefcbce2083b0315aae9854ad5c761304439))


### Performance Improvements

* **tests:** optimize vitest config for faster CI runs ([1a1f049](https://github.com/dynamisinc/cadence/commit/1a1f0495adbc685bf154269f5c83b3e0bae8d91f))

## [1.1.0](https://github.com/dynamisinc/cadence/compare/frontend-v1.0.0...frontend-v1.1.0) (2026-02-01)


### Features

* add clock-based inject workflow with time-based sections ([aec36bf](https://github.com/dynamisinc/cadence/commit/aec36bf729d65e68836545b998bc3ab5061e4cd3))
* Add Exercise Capabilities, Metrics, and Settings features ([c45d565](https://github.com/dynamisinc/cadence/commit/c45d565764f9a414d1043d038d953e958aa4d82c))
* add exercise conduct and observations (Phase D+E) ([d2aae66](https://github.com/dynamisinc/cadence/commit/d2aae66b87ffc749ba6938c6fc3fd3e6dfe46598))
* add HomePage with role-aware dashboard ([993f13a](https://github.com/dynamisinc/cadence/commit/993f13a6ba46f672a552ee7d5e8093ddf10d6aaa))
* add HomePage with role-aware dashboard ([a5d1447](https://github.com/dynamisinc/cadence/commit/a5d144723c7c46e5967801b4a2daafc15202338e))
* add inject organization (sort, filter, group, search) ([4916bf1](https://github.com/dynamisinc/cadence/commit/4916bf1dc8d637d0a64dcb81282fc6de734c8471))
* add inject organization (sort, filter, group, search) ([70ce9f5](https://github.com/dynamisinc/cadence/commit/70ce9f58c88fd8a0b946cc611127aeffa5a9832c))
* add inject reorder API endpoint with SignalR notification ([9af8a27](https://github.com/dynamisinc/cadence/commit/9af8a27a12bdf5415d9a2f4f4f35eaa75573d6db))
* add objectives CRUD, inject-objective linking, timezone expansion, and practice mode ([7898b77](https://github.com/dynamisinc/cadence/commit/7898b77a6bdf63017cda3e600ba5d2787133fb0b))
* add observation-to-inject click navigation ([4d4a66e](https://github.com/dynamisinc/cadence/commit/4d4a66e4e4bbe1e05db84cc8ec29898473351fe3))
* add optimistic updates for observations and clock mutations ([be4077c](https://github.com/dynamisinc/cadence/commit/be4077cc2c3d60e7ff4397a1bebbe98c913bac8d))
* add Phase M.2 - exercise status workflow, MSEL management, and duplication ([6cb1f12](https://github.com/dynamisinc/cadence/commit/6cb1f12740d4a05ed269474254a096bdf496ecc2))
* add PWA support and clock sync on reconnection (Phase I) ([f41c672](https://github.com/dynamisinc/cadence/commit/f41c67201e89ca8b1fad9b6b4cb203e3d3d88dee))
* add real-time sync and offline capability (Phase H) ([0f08b08](https://github.com/dynamisinc/cadence/commit/0f08b0814b8046f4482728f0d876844a6d8306c6))
* add visible Close button to inject detail drawer ([c37745a](https://github.com/dynamisinc/cadence/commit/c37745ab01e9c5f74adf1659cfff5e663fd5da2d))
* **auth:** add useSystemPermissions hook for system-level permissions ([6d63362](https://github.com/dynamisinc/cadence/commit/6d633625612cc20bc16fcb544d58985461dc936d))
* **capabilities:** add Capability Library admin UI (S02) ([783c337](https://github.com/dynamisinc/cadence/commit/783c337cac0876911fcd64dc6ea0c97021f0ad47))
* **capabilities:** add reactivation and fix import refresh ([e6ea549](https://github.com/dynamisinc/cadence/commit/e6ea54982f2d728de208ad0ebde5a33410015b39))
* **capabilities:** implement exercise capabilities Phase 2 (S03, S04, S05, S06) ([4a4e31d](https://github.com/dynamisinc/cadence/commit/4a4e31d0d89f410f2eb99a3efda55a124697046e))
* **capabilities:** refactor CoreCapability to organization-scoped Capability entity (S01) ([8419a6f](https://github.com/dynamisinc/cadence/commit/8419a6f39a239c885fa57075cc406c5cf2c8c148))
* Complete Authentication & Authorization System (S01-S15, S25) ([bacc65a](https://github.com/dynamisinc/cadence/commit/bacc65a95f3bb1a008d26f1b77df834d593df32e))
* complete Excel export integration with enhanced template ([a77c56a](https://github.com/dynamisinc/cadence/commit/a77c56a8f0c9a4194d83d918dbe8793d59a95492))
* Complete Excel export integration with enhanced template ([fc9afca](https://github.com/dynamisinc/cadence/commit/fc9afcaee8fc0587c32a6b1f33401ab65057765a))
* complete Exercise CRUD feature with full-stack implementation ([4245936](https://github.com/dynamisinc/cadence/commit/4245936f2d80696cd9f15ab9e65902c771775833))
* complete MVP implementation with auth fixes and test coverage ([99bc784](https://github.com/dynamisinc/cadence/commit/99bc784002dae7afb633a840410010fca8bd58d9))
* Conduct Page UX Improvements ([408591f](https://github.com/dynamisinc/cadence/commit/408591f89873b5ae3604e577251bf7029e1bb31f))
* **deploy:** update Azure Web App name instructions and improve backend change detection logic ([264c75c](https://github.com/dynamisinc/cadence/commit/264c75c47e3e806e4c78a1a071d580a12b853de1))
* enhance user experience with breadcrumb navigation and improve role display in ProfileMenu ([23a3de3](https://github.com/dynamisinc/cadence/commit/23a3de3e8dccd1bd3bbcf3c65d1737cdc1b143b2))
* Exercise Clock Modes and MSEL Drag-Drop Reordering ([cdb3aaa](https://github.com/dynamisinc/cadence/commit/cdb3aaadc0ec60d0bb6c59c550863fe9834e7ca9))
* exercise conduct with clock-based inject workflow ([b4063ea](https://github.com/dynamisinc/cadence/commit/b4063eaf88e8d44623ead6fdb9935c5c78314ab1))
* Exercise Observations enhancements and UX improvements ([bd8388d](https://github.com/dynamisinc/cadence/commit/bd8388daa5f937da1e8931eb111224500dc1d5c2))
* **exercises:** consolidate exercise lists into shared ExerciseTable component ([a3a8caf](https://github.com/dynamisinc/cadence/commit/a3a8caf922704c920f8884aae742daa0cd9554a7))
* implement authentication and authorization system ([489ad03](https://github.com/dynamisinc/cadence/commit/489ad0398640c1e55cd61c8d10f0b57026bae68d))
* implement Exercise Clock Modes feature (CLK-01 through CLK-10) ([a77af4f](https://github.com/dynamisinc/cadence/commit/a77af4f974b77a43bef6afdc22e8365d12e76dd1))
* implement exercise participant management and role resolution UI (S14-S15) ([d855f12](https://github.com/dynamisinc/cadence/commit/d855f12be74f96afcdd6eaff83b0a52b061180a6))
* implement in-exercise navigation context (S03, S04) ([57cf1b2](https://github.com/dynamisinc/cadence/commit/57cf1b283fdf1bb28d3504c25b15c552eb9b8f2b))
* implement Inject CRUD and Phase Management with comprehensive tests ([9d6a252](https://github.com/dynamisinc/cadence/commit/9d6a252832f9c4c9dac9759727332622579a225d))
* implement inline user creation from exercise participants (S25) ([d78b8bc](https://github.com/dynamisinc/cadence/commit/d78b8bc3b6219d2d0583bc0ca7ed7e9eb3452514))
* implement My Assignments and Notifications features (P0-03, P0-04) ([b720364](https://github.com/dynamisinc/cadence/commit/b72036419a474b46d677fca16aba05ba315a97ea))
* implement navigation shell foundation with role-based menu ([28844d7](https://github.com/dynamisinc/cadence/commit/28844d77fc3b1d3eb135e78260120f119635feef))
* implement Reports & Export feature (P0-05) ([f5651e4](https://github.com/dynamisinc/cadence/commit/f5651e466a61c09df764b19de0f6fe067d0e9487))
* implement sidebar navigation, assignments, notifications, and reports (P0-03, P0-04, P0-05) ([3b464d5](https://github.com/dynamisinc/cadence/commit/3b464d5c186e87daa9c25039c504715b3068d192))
* improve conduct page UX with layout options and better scrolling ([d7a03af](https://github.com/dynamisinc/cadence/commit/d7a03af53d3ea95e5c243292a94d96cd49b1ffa1))
* improve exercise detail page layout and add URL-based edit mode ([1a66902](https://github.com/dynamisinc/cadence/commit/1a669029d8cc0f3201b7cacda355aaae3b4b464a))
* MSEL Import from Excel with streamlined exercise creation ([abe9cb4](https://github.com/dynamisinc/cadence/commit/abe9cb44ff0aeb575a2b6ab57890c6ea0f194e29))
* MSEL import from Excel with streamlined exercise creation flow ([6390e71](https://github.com/dynamisinc/cadence/commit/6390e71426227a72be0e434aae0dd9b7d28b9927))
* **msel:** add edit and delete buttons for injects on MSEL page ([aff9d91](https://github.com/dynamisinc/cadence/commit/aff9d918f33676155ec44bb88b3b4cd7783b4974))
* **observations:** add capability tagging to conduct page ([61b6cb0](https://github.com/dynamisinc/cadence/commit/61b6cb0841888862354e5c80f4f6a9cd4bdec7e3))
* **observations:** enhance observation workflow with standalone page and UX improvements ([3edfcb5](https://github.com/dynamisinc/cadence/commit/3edfcb5582f2f0e1469483c3c8c64a1998f93b48))
* Organization Management with Multi-Tenant Data Isolation ([186bc14](https://github.com/dynamisinc/cadence/commit/186bc1459d7248aa1671ea0194f9a782742b0357))
* **organizations:** add member management and integration tests ([3277db7](https://github.com/dynamisinc/cadence/commit/3277db7ae71dac707a7f9a9458b1a63a7f259c3b))
* **organizations:** add organization scoping to all entities ([482b405](https://github.com/dynamisinc/cadence/commit/482b405371bb2b2fbd031c7d25725cbdcb67b76e))
* **organizations:** implement data isolation for multi-tenant security ([9cc7dc6](https://github.com/dynamisinc/cadence/commit/9cc7dc607b57312faeca4c42d9363952550b93d8))
* Phase G (Inject Organization) + Phase H (Real-Time Sync & Offline) ([46f2159](https://github.com/dynamisinc/cadence/commit/46f2159bee597a2d0dbc9ac7cc3d62780f5a76a3))
* Phase M - Objectives, timezone expansion, and practice mode ([714a51a](https://github.com/dynamisinc/cadence/commit/714a51abeb8c3c68f692ae0177f6b4cacbc12437))
* Phase M.2 - Exercise Status Workflow, MSEL Management & Optimistic Updates ([aaecfd7](https://github.com/dynamisinc/cadence/commit/aaecfd75cc4e1b7e93185b6faac1d825dcf136d1))
* **settings-metrics:** add settings/metrics feature specs and confirmation dialogs ([b3a97f2](https://github.com/dynamisinc/cadence/commit/b3a97f2e9e48d23f275bd1c655fb044953663ad9))
* **settings:** persist confirmation skip preferences in localStorage ([65289b2](https://github.com/dynamisinc/cadence/commit/65289b2341c765a8275fcae96185f00efb0630e3))
* **settings:** update permissions and add exercise settings defaults ([ecd8c10](https://github.com/dynamisinc/cadence/commit/ecd8c10598ebbf775d9a3bdccd7dc19f3f14830c))
* **settings:** wire confirmation dialogs to exercise settings ([00e672c](https://github.com/dynamisinc/cadence/commit/00e672cc4c1b4460afdf779c54f28c54df76c2bb))
* **ui:** add versioning and release notes feature ([262f884](https://github.com/dynamisinc/cadence/commit/262f8849706dd3171269edd8429b62912bba93b4))
* **ui:** improve exercise list UX with role display and header cleanup ([03f194e](https://github.com/dynamisinc/cadence/commit/03f194edc79ce061ae77590c77b20d7cd7dc8a0d))


### Bug Fixes

* add inject number to observations and implement edit functionality ([64bac9a](https://github.com/dynamisinc/cadence/commit/64bac9a0a9eb3a9bb281d2d154fae907e74b3f1d))
* add missing API health check mock in ConnectivityContext tests ([69dff5d](https://github.com/dynamisinc/cadence/commit/69dff5d4c7ffee80397e1bcbfb7dbba3847c9669))
* add proper breadcrumbs with exercise name to sub-pages and placeholder routes ([5c063d0](https://github.com/dynamisinc/cadence/commit/5c063d0d1f4cf4a39a33fa098003d7925c27cbe2))
* add SignalR notifications to InjectsController and InjectStatusChanged event ([3a0f3dd](https://github.com/dynamisinc/cadence/commit/3a0f3dd80236d3bd5bcff1f4813dc0a5d896a3e9))
* address code review findings from PR [#14](https://github.com/dynamisinc/cadence/issues/14) ([baa967f](https://github.com/dynamisinc/cadence/commit/baa967f3a5dba66f828e0c9a9fc138414e2165e4))
* address code review issues for organization management ([fde84f9](https://github.com/dynamisinc/cadence/commit/fde84f9dba3a7518df226151c2a8eaec83c7784b))
* address code review issues for PR [#13](https://github.com/dynamisinc/cadence/issues/13) ([fe8ea9c](https://github.com/dynamisinc/cadence/commit/fe8ea9ca7adf58511556ff701fcf3496a0bd5ebf))
* address code review issues from PR 15 ([2e7fc6a](https://github.com/dynamisinc/cadence/commit/2e7fc6a46dd1354d8b0a563565a6d1fb3d9801d0))
* always show all assignment sections even when empty ([d3199f6](https://github.com/dynamisinc/cadence/commit/d3199f6df22dd8a23c049827cbed3b089f84f7f8))
* **api:** resolve compiler warnings in backend and frontend ([2c8b7d8](https://github.com/dynamisinc/cadence/commit/2c8b7d82bf03d8f8859c05325d8afb5b1abe151a))
* **auth:** align ExerciseAssignmentDto with backend response ([6f95fdd](https://github.com/dynamisinc/cadence/commit/6f95fdd1eeec5fdf953c47941aa0aa71e316e8a8))
* **auth:** replace deprecated usePermissions with useExerciseRole ([14604b5](https://github.com/dynamisinc/cadence/commit/14604b5b6411a4268f64608798444b399033375e))
* **auth:** resolve infinite loops and improve token refresh resilience ([c127029](https://github.com/dynamisinc/cadence/commit/c12702941c2102a8bd7383722495925b2b5c1451))
* centralize drag-drop saving indicator to prevent double display ([2002123](https://github.com/dynamisinc/cadence/commit/20021231db7c009706d4c38049eccd51648472f0))
* CLAUDE.md compliance and eslint errors ([1bfb645](https://github.com/dynamisinc/cadence/commit/1bfb64540a318116d69991b84d1e32be5772b76d))
* **conduct:** wire up reset and skip inject actions from drawer ([d96ca62](https://github.com/dynamisinc/cadence/commit/d96ca62d5639bd66aa1d7a074eb07f1542b1e50e))
* correct FilterType value in ActiveFiltersBar test ([8f90357](https://github.com/dynamisinc/cadence/commit/8f90357d4ef3324f3585facfb00f5b5d993a5c99))
* correct indentation and trailing comma in ExerciseDetailPage ([78e3ea2](https://github.com/dynamisinc/cadence/commit/78e3ea248f7ce3564ebd7cc35b7161f6358ab3c9))
* correct vite-plugin-pwa property name from 'disabled' to 'disable' ([b4134cc](https://github.com/dynamisinc/cadence/commit/b4134cc8841e9e79016b78c7b957aebc54b5645b))
* display correct objective names in active filter labels ([e4a428f](https://github.com/dynamisinc/cadence/commit/e4a428f74b981cf9a64d6c6d20cc47cef378b3ea))
* **exercise-clock:** comply with Rules of Hooks in confirmation dialog ([9ba3b50](https://github.com/dynamisinc/cadence/commit/9ba3b50ac45e1024973de07454941ecd68a48d93))
* **exercises:** improve Exercise Director dropdown and offline handling ([4bb5ad7](https://github.com/dynamisinc/cadence/commit/4bb5ad71b706b00f7b06e7912503af541ccdefda))
* **frontend:** improve offline auth handling and fix console errors ([9c0e451](https://github.com/dynamisinc/cadence/commit/9c0e4519fea5a556da9be95c8a10d0da76376b85))
* **frontend:** resolve ESLint errors and .NET formatting issues ([58102ce](https://github.com/dynamisinc/cadence/commit/58102cec14f7d316af87cb11a50dab4230b679ff))
* **frontend:** resolve eslint errors for CI compliance ([a7705ea](https://github.com/dynamisinc/cadence/commit/a7705ea77b7dbb0f7395fecec5f55575c72dd81c))
* **frontend:** resolve ESLint max-len and unused variable warnings ([82dcc42](https://github.com/dynamisinc/cadence/commit/82dcc42917288eae2913b30a07f16295373d73e1))
* hide Complete Exercise option for Archived exercises ([ba29f5a](https://github.com/dynamisinc/cadence/commit/ba29f5afce20fa44508b7dcae6ef3abee196d253))
* improve 404 page UX with clean URL and attempted path display ([921971c](https://github.com/dynamisinc/cadence/commit/921971c6c94d9b616b6159997208bcd21127dce2))
* improve content-disposition parsing and add deprecation warning ([c5d4f94](https://github.com/dynamisinc/cadence/commit/c5d4f946d1ea79dafd539a03395909818c955535))
* **injects:** add missing imports to InjectForm ([6a0675f](https://github.com/dynamisinc/cadence/commit/6a0675f8419b083febef48faa9677b26d1d82ba2))
* make AssignmentSection collapsible with completed collapsed by default ([ead30c6](https://github.com/dynamisinc/cadence/commit/ead30c6e6fa2f9b8969634915667dbc3d1d66532))
* make usePhases tests more robust with waitFor assertions ([9e4383a](https://github.com/dynamisinc/cadence/commit/9e4383af1134da47686f29a02e7938624ed1fd78))
* multiple bug fixes for exercise management ([cf72b3f](https://github.com/dynamisinc/cadence/commit/cf72b3f6f20ee1f62af1a88bde733f0c64a84142))
* observation FK constraint and reports page UX ([1f217f4](https://github.com/dynamisinc/cadence/commit/1f217f4782926a6b9c58029c90eda1cb9991e1ae))
* **offline:** prevent login redirect when API is unreachable ([e18d023](https://github.com/dynamisinc/cadence/commit/e18d0234055ff33bcd276d59e801af395f466736))
* optimize inject loading performance and enhance conduct views ([e8155dd](https://github.com/dynamisinc/cadence/commit/e8155ddbecba900b80089638ebcc2aed8834c45e))
* **organizations:** complete organization switching and context display ([782d1b4](https://github.com/dynamisinc/cadence/commit/782d1b456b54d907cdd51520aca87a874d197f64))
* prevent duplicate observations from SignalR race condition ([5b60e6a](https://github.com/dynamisinc/cadence/commit/5b60e6a0a2844639dfd82f56fbbc794c81d74350))
* prevent empty state flash during background refetch on exercises list ([0ae0c5b](https://github.com/dynamisinc/cadence/commit/0ae0c5bac9a405e5f5329a28505c1f1da25c587b))
* prevent state updates after unmount in ConnectivityContext ([8f8cc01](https://github.com/dynamisinc/cadence/commit/8f8cc01cae3460c39ccec8a25b9ee1c4df737811))
* remove exerciseId prop from ObservationForm usage ([65e00aa](https://github.com/dynamisinc/cadence/commit/65e00aa94ac6be6f10253837ab7ada4cc4f984bd))
* remove unnecessary parentheses in arrow functions ([255a0eb](https://github.com/dynamisinc/cadence/commit/255a0ebadb5eb52e6c53a99a928954b4efb7d82c))
* remove unused exerciseId prop from ObservationForm ([5ed7cf6](https://github.com/dynamisinc/cadence/commit/5ed7cf6c28a9c53b0df07b751f595f4f87958682))
* render UnsavedChangesDialog on CreateExercisePage ([8abe2cd](https://github.com/dynamisinc/cadence/commit/8abe2cd7a8b309b241f44d3d346bbb084217bcb0))
* replace any type casts with PermissionRole in test ([0483b2a](https://github.com/dynamisinc/cadence/commit/0483b2a04a8fd335f5ddd4ecf75ec9269821cab1))
* resolve CI/CD frontend test failures ([3ed24df](https://github.com/dynamisinc/cadence/commit/3ed24df3254235538fdc21c673c4e80ac920e170))
* resolve eslint errors and line length warnings ([0dadf5b](https://github.com/dynamisinc/cadence/commit/0dadf5b4bab270e02cd8ad7028bc09c7ee6a231b))
* resolve eslint errors and unused variable warnings ([522fa57](https://github.com/dynamisinc/cadence/commit/522fa57a9f23de3acc016d95016fab97b2c71060))
* resolve ESLint errors for unused imports and variables ([78edef9](https://github.com/dynamisinc/cadence/commit/78edef968e8a31d96b38ba670aab592cd9aa4060))
* resolve ESLint errors in inject organization feature ([ce4bb4d](https://github.com/dynamisinc/cadence/commit/ce4bb4ddbee56d1171fa752c8846ce0f8970ba91))
* resolve eslint no-explicit-any error in ResetPasswordPage test ([113d82a](https://github.com/dynamisinc/cadence/commit/113d82a59ce942d15b353b4ff3d66e56fd825e25))
* resolve exercise participants and details page issues ([5e0f1ba](https://github.com/dynamisinc/cadence/commit/5e0f1ba62d96bd4177032b0508ecace4bd2484d4))
* resolve frontend test failures related to S25 implementation ([86b3ce2](https://github.com/dynamisinc/cadence/commit/86b3ce2fe60c8579e22741ca588f592d2e79424e))
* resolve frontend TypeScript build errors ([38c4e5c](https://github.com/dynamisinc/cadence/commit/38c4e5c1362764ea072fb7385281dfe62ddfaee6))
* resolve JWT refresh token cookie and race condition issues ([1d517d2](https://github.com/dynamisinc/cadence/commit/1d517d233c4661dc598470e80813b530ef000074))
* resolve line length lint warnings ([e296f45](https://github.com/dynamisinc/cadence/commit/e296f4589dff0cf0706c428b2e9011ad381460fc))
* resolve lint errors in Phase M.2 code ([244b607](https://github.com/dynamisinc/cadence/commit/244b607b5a726b2406913e0708aa7873c58613fc))
* resolve lint errors in test files ([4ccd2e3](https://github.com/dynamisinc/cadence/commit/4ccd2e3a7824f2e718beb59b95d3f45467aa90f9))
* resolve remaining lint errors for CI/CD ([f082ddf](https://github.com/dynamisinc/cadence/commit/f082ddf9be3b2f42f339b6ab2585ef113e2a8a7c))
* resolve test timeouts and label text matching issues ([6d6f675](https://github.com/dynamisinc/cadence/commit/6d6f67563ba46d6e84b3e140b58c5e0a99300ee0))
* resolve test timeouts in CI environment ([dafa493](https://github.com/dynamisinc/cadence/commit/dafa4939817daaf19bd59a5284deaf90dc489e35))
* resolve TypeScript build errors across frontend ([cf99a3a](https://github.com/dynamisinc/cadence/commit/cf99a3ac2facfa43c7b58e7a59c2481875f09487))
* resolve TypeScript build errors in frontend ([4b01e46](https://github.com/dynamisinc/cadence/commit/4b01e46d8ec759cf27908e6964b092eb56d55266))
* resolve TypeScript build errors in test fixtures and DTOs ([80a9c5f](https://github.com/dynamisinc/cadence/commit/80a9c5fa1097c1764f0c25a795327900bb7de632))
* resolve TypeScript errors in mock factories and DTO types ([81ed7a4](https://github.com/dynamisinc/cadence/commit/81ed7a45970627cdee8f68a4ab316af2e3fc1cfd))
* resolve TypeScript errors in test files ([891f67d](https://github.com/dynamisinc/cadence/commit/891f67da5b8c34ee251a6caf53dba4dea65afe04))
* resolve TypeScript type errors in test files ([10f5c41](https://github.com/dynamisinc/cadence/commit/10f5c4128a0af9218ea3d0e72ce0faeccbf36a7c))
* resolve TypeScript type mismatches in offline sync feature ([1884bd3](https://github.com/dynamisinc/cadence/commit/1884bd325bbb6b64011c267cdf8b141a8f9e3f9b))
* show exercise name in breadcrumbs instead of ID on hub page ([e7ec186](https://github.com/dynamisinc/cadence/commit/e7ec1869cf93449fe50bd63c05f64cddd9e01e5b))
* **tests:** add eslint-disable for no-explicit-any in test mocks ([c0c35c6](https://github.com/dynamisinc/cadence/commit/c0c35c67ea25530775174ede2c3d1a07fa6eb470))
* **tests:** remove unused 'within' import ([6bd73e1](https://github.com/dynamisinc/cadence/commit/6bd73e18b6ae93b0fae8d937b257085a73378c24))
* **tests:** resolve 13 failing frontend tests ([d682fa5](https://github.com/dynamisinc/cadence/commit/d682fa5c562e7a3923c267ada2109ef697ef5abe))
* **tests:** resolve lint errors and fix failing tests ([1b512b6](https://github.com/dynamisinc/cadence/commit/1b512b608c668cb089fc533fd5549fca242cbf3d))
* **tests:** update failing tests to match component changes ([6434f7e](https://github.com/dynamisinc/cadence/commit/6434f7ebeaac0738d5752e4a339d11f89c36563e))
* TypeScript errors for ExerciseDto and validation types ([594f9ee](https://github.com/dynamisinc/cadence/commit/594f9ee486c874c0253798ab095a3104aef75fc2))
* **types:** resolve TypeScript build errors ([a0c08c7](https://github.com/dynamisinc/cadence/commit/a0c08c77751b48d02a50bc877303b52ffb41cb15))
* **types:** resolve TypeScript compilation errors ([2d79132](https://github.com/dynamisinc/cadence/commit/2d79132d6eed0e49a5b55a91859f94529fc740a5))
* update AddParticipantDialog tests for proper test utils ([730bfb8](https://github.com/dynamisinc/cadence/commit/730bfb85eb261f470f8b0eafc801cb711f3a4fd3))
* update AuthContext test expectation for error response ([89f1e92](https://github.com/dynamisinc/cadence/commit/89f1e9278132efa45c99be319417a45b3b84a8c5))
* update organization page tests for COBRA component compatibility ([c59e2ba](https://github.com/dynamisinc/cadence/commit/c59e2ba285ea1338f6d37ad4959ad043d15e1a4d))
* use empty string instead of null for target field in tests ([031bd9e](https://github.com/dynamisinc/cadence/commit/031bd9e430b715ece560ba0c912beb44c9243c54))
* use import.meta.env.DEV instead of process.env.NODE_ENV ([4b9de5a](https://github.com/dynamisinc/cadence/commit/4b9de5a57320172d5f7618525d6062a08ee642f0))
* use relative path in tsconfig paths without baseUrl ([739e827](https://github.com/dynamisinc/cadence/commit/739e827bc14275b422496e88b9799356c925f95c))
* use type-only imports for FC and add @types/lodash ([c15e014](https://github.com/dynamisinc/cadence/commit/c15e014a41bfb16e388174e23d06a0048564d63a))
* use valid InjectType values in offline cache ([704a811](https://github.com/dynamisinc/cadence/commit/704a8118a71a06eca93a9517d61a27ede2d38344))
* **version:** fix WhatsNewModal rendering and AboutPage layout ([76a1076](https://github.com/dynamisinc/cadence/commit/76a1076dd2d072bb1625524f1cde373b1ebe2ff4))

## [1.0.0] - 2026-01-30

### Features

#### Exercise Management

- Exercise list, create, edit, and detail views with full CRUD operations
- Exercise status workflow (Draft → Active → Completed → Archived) with confirmation dialogs
- Exercise duplication with all related data (phases, objectives, injects)
- Setup progress sidebar for Draft exercises showing completion status
- Practice mode toggle with visual indicator across the application

#### MSEL & Inject Management

- Complete MSEL/Inject management with status tracking (Pending, Fired, Skipped, Deferred)
- Drag-and-drop inject reordering with optimistic updates
- Fire Confirmation Dialog with user preference persistence
- Inject organization features: sorting, filtering, grouping, and full-text search
- State persistence to sessionStorage per exercise
- Inject-to-Objective linking with many-to-many relationships
- Inject detail drawer with description preview and delivery method icons

#### Exercise Conduct

- Real-time exercise clock with start/pause/stop/reset controls
- Time-based inject sections (Ready to Fire, Upcoming, Later, Fired, Skipped)
- Clock-driven and facilitator-paced conduct view modes
- Narrative view for Observers showing "The Story So Far" and "What's Happening Now"
- Layout mode toggle (Classic, Sticky Header, Floating Chip)
- Progress indicator with current phase name and completion bar

#### Observations

- Observations panel for evaluators during active exercises
- Quick entry with keyboard shortcuts (Ctrl+O or O)
- Observation-to-inject linking with clickable references
- Location field for documenting where observations occurred

#### Capabilities & Metrics

- Organization-scoped capability library (FEMA, NATO, NIST CSF 2.0, ISO 22301)
- Admin UI for managing capabilities and importing predefined libraries
- Exercise metrics dashboard with progress tracking
- Inject summaries (P/S/M/U ratings) and observation summaries
- Timeline analysis and capability performance metrics

#### Settings & User Preferences

- User preferences (time format, theme)
- Exercise settings (confirmation dialogs, auto-fire, clock mode)
- Timing configuration (start time, scenario time, time ratio)
- Expanded timezone support (67 global timezones)

#### Authentication & Authorization

- JWT-based authentication with access tokens and refresh tokens
- Remember Me support with extended refresh token lifetime
- Cross-tab logout synchronization
- Role-based access control with three-tier hierarchy:
  - System Roles: Admin, Manager, User
  - Organization Roles: OrgAdmin, OrgManager, OrgUser
  - Exercise Roles: Observer, Evaluator, Controller, ExerciseDirector
- User management with search, role filtering, and deactivation
- Exercise participant management with HSEEP role assignment
- Inline user creation from Add Participant dialog

#### Navigation & UX

- Role-based sidebar and header navigation with collapsible menus
- In-exercise navigation with role-filtered menu items
- Breadcrumb navigation throughout the app
- My Assignments dashboard showing exercises grouped by status
- Profile menu with role display and exercise assignments

#### Reports & Export

- Excel export for MSEL data and observations
- Full exercise package export (ZIP with MSEL, Observations, Summary)
- Excel import wizard (upload → sheet selection → column mapping → validation → import)
- Intelligent column mapping with auto-suggestions
- Template download with Instructions and Lookups worksheets

#### Real-Time & Offline

- SignalR-based live updates for injects, observations, and clock state
- Offline detection with combined browser online + SignalR state monitoring
- IndexedDB caching via Dexie.js for injects and observations
- Action queue with FIFO processing on reconnect
- Conflict resolution with user notification dialog
- Force-push capability on reconnect for offline sync

#### PWA Support

- Progressive Web App support for installation
- Responsive design for tablet and desktop

### Bug Fixes

- Fixed auth handling when API is unreachable
- Fixed character counter showing `[object Object]` in exercise name field
- Fixed file picker opening twice when clicking Browse Files
- Fixed unsaved changes warning appearing after successful exercise creation
- Fixed InjectTypeChip null check error
- Fixed duplicate observation prevention from SignalR race conditions
- Fixed empty state flash on page load
- Fixed objective filter label display
- Fixed vite.config.ts and useInjects.ts indentation errors
- Added liveness endpoint (/api/health/live) for deployment validation

### Technical

- React 19 with TypeScript 5.x
- Material UI 7 component library with COBRA styling system
- Vite 7 build tooling
- .NET 10 backend with Entity Framework Core
- Azure App Service hosting (always warm, no cold starts)
- Azure SignalR Service for real-time communication
- Azure SQL Database
- FontAwesome icons
- Vitest + React Testing Library (2500+ tests)
