# 📱 دليل بناء لعبة Fotbool للهواتف المحمولة

## المتطلبات

- Unity 2022.3 LTS أو أحدث
- Android SDK (لبناء Android)
- Xcode (لبناء iOS على macOS)
- Java Development Kit (JDK) 8 أو 11

## إعدادات البناء

### Android
1. افتح Unity Hub وأضف وحدة Android Build Support
2. تأكد من تثبيت Android SDK و NDK عبر Unity Hub
3. اذهب إلى `File → Build Settings` واختر `Android`
4. انقر على `Switch Platform`
5. اذهب إلى `Player Settings` وحدد:
   - Bundle Identifier: `com.fotbool.game`
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Android 13 (API 33)

### iOS
1. يتطلب macOS و Xcode مثبتان
2. اذهب إلى `File → Build Settings` واختر `iOS`
3. انقر على `Switch Platform`
4. في `Player Settings` حدد:
   - Bundle Identifier: `com.fotbool.game`
   - iOS Target Version: 12.0 أو أحدث

## طريقة البناء السريع

### باستخدام قائمة Unity (الموصى به)
1. افتح المشروع في Unity
2. اذهب إلى قائمة الكبار: **Fotbool → Build**
3. اختر:
   - `Android APK` لبناء تطبيق أندرويد
   - `Android APK (Development)` لبناء تطويري مع أدوات التصحيح
   - `iOS` لبناء تطبيق آيفون

سيتم حفظ الملفات في مجلد `Builds/` في جذر المشروع.

### باستخدام سطر الأوامر
```bash
# بناء Android
/Applications/Unity/Hub/Editor/2022.3.x/Unity -quit -batchmode -projectPath d:/Fotbool -executeMethod Fotbool.Editor.MobileBuildScript.BuildAndroid

# بناء iOS
/Applications/Unity/Hub/Editor/2022.3.x/Unity -quit -batchmode -projectPath d:/Fotbool -executeMethod Fotbool.Editor.MobileBuildScript.BuildiOS
```

## إعداد واجهة المستخدم (UI)

قبل البناء، تأكد من إعداد اللوحات التالية في المشهد:

| اللوحة | السكربت المرفق | الوصف |
|--------|----------------|-------|
| KineticIntroPanel | KineticIntroScreen | الشاشة الافتتاحية الحركية |
| WelcomePanel | WelcomeScreen | شاشة الترحيب الرئيسية |
| GameIntroductionPanel | GameIntroductionScreen | صفحة التعريف باللعبة |
| MainMenuPanel | UIManager | القائمة الرئيسية |
| MatchHUDPanel | UIManager | واجهة أثناء المباراة |

### خطوات الإعداد في المحرر:
1. أنشئ Canvas جديد في المشهد
2. أضف Panels لكل شاشة (Intro, Welcome, Introduction, MainMenu, MatchHUD)
3. أضف السكربطات المقابلة لكل لوحة
4. اربط الحقول في Inspector (النصوص، الأزرار، الصور)
5. اسحب prefab Canvas إلى DontDestroyOnLoad إذا لزم الأمر

## اختبار البناء على الجهاز

### Android
1. فعّل خيار "تصحيحUSB" على جهاز أندرويد
2. قم بتوصيل الجهاز بالكمبيوتر عبر USB
3. في Unity: `File → Build and Run`
4. سيتم تثبيت التطبيق وتشغيله تلقائياً

### iOS
1. قم ببناء المشروع لـ iOS
2. افتح المشروع الناتج في Xcode
3. اختر جهازك المستهدف وانقر على Run

## استكشاف الأخطاء

| المشكلة | الحل |
|---------|------|
| خطأ في Bundle Identifier | تأكد من أن المعرف فريد ولا يحتوي على مسافات |
| خطأ في SDK | ثبت Android SDK عبر Unity Hub |
| التطبيق ينهار عند البدء | تأكد من أن مشهد البداية مضاف إلى Build Settings |
| خطأ في التوقيع (iOS) | قم بتسجيل الدخول إلى Apple ID في Xcode |

## هيكل ملفات البناء

```
Builds/
├── Fotbool.apk              # تطبيق أندرويد
├── Fotbool_Development.apk  # إصدار تطويري
└── Fotbool_iOS/             # مشروع iOS لـ Xcode
```

---

**فريق Fotbool Studio** - © 2026