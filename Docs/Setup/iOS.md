# iOS build plan — not configured

Development takes place on Windows with no local Mac. Proposed route:
GitHub revision → hosted Unity/macOS build → Xcode signing → App Store Connect →
TestFlight → iPhone. Unity Build Automation is the current recommendation, not a
purchased or activated service. GitHub remains the source repository.

Before enabling builds:

1. Complete and commit the verified Windows Editor import.
2. Confirm the hosted service offers Unity 6000.3.21f1 and a suitable Xcode/SDK pair.
3. Check current pricing, Unity licensing and account access before activating usage.
4. Set up Apple Developer Program/App Store Connect access for TestFlight; select
   the final app/bundle identifier explicitly. No identifier is reserved here.
5. Configure certificates, profiles and API keys in secure service credentials.
   Never put them in Git, source files, issues or PR bodies.
6. Configure GitHub access with the needed scope and LFS checkout. Confirm actual
   asset bytes are available, rather than LFS pointer text.
7. Configure signing and upload explicitly. A successful compile alone does not
   mean a build has reached TestFlight. Begin with an on-demand build.

Cloud builds do not impose an online requirement on installed solo gameplay.
Cloud sync, purchases and future PvP remain separate service boundaries.

First device checks include portrait safe areas, thermal/frame-time measurements,
background/resume, Control Center/audio interruption and offline launch. Unity's
6000.3.21f1 release notes list iOS/Metal issues; record reproduction on our build
before choosing mitigation or an explicit engine-version change.

Sources checked 2026-09-25:
- https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- https://docs.unity.com/en-us/build-automation/basic-build-configuration/set-up-an-ios-build-configuration
- https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases
- https://unity.com/releases/editor/whats-new/6000.3.21f1
