# مسار بناء Unity وAPK — Octopus S1

**المحرّك المثبت للمشروع:** Unity 6000.3.25f1 (Unity 6.3 LTS).  
المصدر: صفحة إصدار Unity الرسمية ورقم التغييرات المرفق مع هذا الإصدار.

## هيكل النموذج الأولي

- `Assets/Scripts/OctopusS1Prototype.cs`: مشهد اختبار يتولد عند التشغيل، مع حركة، رفيق أخطبوط مؤقت، ثلاث خلايا، برج إرسال ونقطة إخلاء.
- `Assets/Editor/OctopusS1SceneBuilder.cs`: من قائمة Unity اختر `Octopus S1 → Build Prototype Scene` لإنشاء مشهد `Assets/Scenes/OctopusS1Prototype.unity` وإضافته إلى Build Settings.
- كل الهندسة في هذا النموذج **Placeholder**. لا تمثل جودة فنية أو أصولًا نهائية.
- ضوابط الاختبار: WASD/الأسهم وQ/E على سطح المكتب؛ أزرار لمس أولية على الشاشة.

## متطلبات Android

لبناء تطبيق Android من Unity، توثق Unity اعتماد الوحدة Android Build Support وAndroid SDK وAndroid NDK وJDK. تتولى نسخة Unity المختارة تحديد النسخ المتوافقة. يثبت Android Build Support مع Unity Hub/Editor بدل تركيب NDK عشوائيًا. بعد البناء نختبر APK على جهاز حقيقي، فالمحاكي أو نجاح Gradle لا يكفي لقبول G3.

## خطوات التطوير في Codespace

1. تثبيت Unity 6000.3.25f1 على عامل Linux/Windows يدعمها، مع Android Build Support.
2. توفير ترخيص Unity بالطريقة المدعومة. لا يودع ملف الترخيص أو الأسرار في Git.
3. في محرر Unity، تشغيل `Octopus S1 → Build Prototype Scene`.
4. اختيار Android وبناء APK تجريبي.
5. تثبيت APK على جهاز Android، تسجيل النسخة والجهاز والالتزام، وتوثيق سيناريو المهمة والفشل والأداء.
6. إصلاح الملاحظات وإعادة القياس قبل اعتماد G3.

بيئة Codespace الحالية لا يظهر فيها أمر Unity أو adb ضمن PATH. لذلك هذا الدليل إعداد مقترح؛ لا يثبت أن المحرك أو وحدات Android موجودة في البيئة، ولا يسجل نجاح بناء.

## السجل

| التاريخ (UTC) | الالتزام | المحرك | النتيجة |
|---|---|---|---|
| 2026-10-26 | فرع التأسيس | 6000.3.25f1 مقترح/مقيد في المشروع | لم يجرِ compile أو build أو تشغيل بعد |
