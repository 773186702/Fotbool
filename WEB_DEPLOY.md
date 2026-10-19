# 🌐 نشر لعبة Fotbool على المتصفح (WebGL)

## الخطوات السريعة

### الطريقة 1: Netlify (مجاني وسريع)

1. افتح Unity وابدأ المشروع
2. اذهب إلى: **Fotbool → Build → WebGL**
3. سيُنشأ مجلد `Builds/WebGL`
4. اذهب إلى [netlify.com](https://netlify.com)
5. اسحب وأفلت مجلد `WebGL` في منطقة النشر
6. سيحصل موقعك على رابط مباشر مثل: `https://fotbool-game.netlify.app`

### الطريقة 2: Vercel (مجاني)

1. ارفع مجلد `Builds/WebGL` إلى مستودع GitHub
2. ربط المستودع بـ [vercel.com](https://vercel.com)
3. سيقوم Vercel بالنشر تلقائياً

### الطريقة 3: GitHub Pages (مجاني)

1. أنشئ مستودع جديد على GitHub
2. ارفع محتويات مجلد `Builds/WebGL` إلى الفرع `gh-pages`
3. فعّل GitHub Pages من إعدادات المستودع
4. الرابط سيكون: `https://username.github.io/repository-name`

## تشغيل اللعبة على الموبايل

بعد النشر:
1. افتح الرابط في متصفح الموبايل (Chrome أو Safari)
2. انقر على قائمة المتصفح (⋮)
3. اختر **"إضافة إلى الشاشة الرئيسية" (Add to Home Screen)**
4. ستصبح اللعبة كتطبيق على شاشة الموبايل

## هيكل الملفات بعد البناء

