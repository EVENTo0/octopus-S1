using System.Collections.Generic;

/// <summary>Arabic / English UI strings for the first mission.</summary>
public static class OctopusS1Text
{
    private static readonly Dictionary<string, string> English = new Dictionary<string, string>
    {
        { "title", "OCTOPUS S1" },
        { "subtitle", "First mission prototype - placeholder blockout art" },
        { "start", "Start mission" },
        { "continue", "Continue mission" },
        { "help", "How to play" },
        { "settings", "Settings" },
        { "back", "Back" },
        { "paused", "Paused" },
        { "resume", "Resume" },
        { "restart", "Restart mission" },
        { "menu", "Main menu" },
        { "recover", "Return to start point" },
        { "interact", "Interact" },
        { "scan", "Scan" },
        { "language", "Language: English" },
        { "sound_on", "Sound: On" },
        { "sound_off", "Sound: Off" },
        { "volume", "Volume" },
        { "obj_collect", "Collect relay cells: {0} / 3" },
        { "obj_relay", "Activate the relay tower" },
        { "obj_extract", "Reach the extraction point" },
        { "obj_done", "Mission complete" },
        { "cell_got", "Relay cell collected" },
        { "need_cells", "The relay needs 3 cells first" },
        { "relay_on", "Relay online - head to extraction" },
        { "need_relay", "Activate the relay before extraction" },
        { "nothing", "Nothing to interact with here" },
        { "recovered", "Returned to the start point" },
        { "scanning", "Scanning - relay cells highlighted" },
        { "result_title", "Mission complete - signal restored" },
        { "result_stats", "Time: {0}s   Best: {1}s   Missions completed: {2}" },
        { "play_again", "Play again" },
        { "help_body", "Drag on the left side to move. Drag on the right side to turn the camera.\n" +
                       "Scan highlights the 3 relay cells. Collect them, activate the relay tower, then reach the extraction point.\n" +
                       "Keyboard: WASD or arrows to move, Q scan, E interact, Esc pause." },
    };

    private static readonly Dictionary<string, string> Arabic = new Dictionary<string, string>
    {
        { "title", "أخطبوط S1" },
        { "subtitle", "نموذج المهمة الأولى - رسوم مؤقتة" },
        { "start", "ابدأ المهمة" },
        { "continue", "تابع المهمة" },
        { "help", "طريقة اللعب" },
        { "settings", "الإعدادات" },
        { "back", "رجوع" },
        { "paused", "إيقاف مؤقت" },
        { "resume", "استئناف" },
        { "restart", "إعادة المهمة" },
        { "menu", "القائمة الرئيسية" },
        { "recover", "العودة إلى نقطة البداية" },
        { "interact", "تفاعل" },
        { "scan", "كشف" },
        { "language", "اللغة: العربية" },
        { "sound_on", "الصوت: يعمل" },
        { "sound_off", "الصوت: متوقف" },
        { "volume", "مستوى الصوت" },
        { "obj_collect", "اجمع خلايا الإرسال: {0} / 3" },
        { "obj_relay", "فعل برج الإرسال" },
        { "obj_extract", "توجه إلى نقطة الإخلاء" },
        { "obj_done", "اكتملت المهمة" },
        { "cell_got", "تم جمع خلية إرسال" },
        { "need_cells", "البرج يحتاج 3 خلايا أولا" },
        { "relay_on", "البرج يعمل - توجه إلى الإخلاء" },
        { "need_relay", "فعل البرج قبل الإخلاء" },
        { "nothing", "لا شيء للتفاعل هنا" },
        { "recovered", "أعدت إلى نقطة البداية" },
        { "scanning", "جار الكشف - الخلايا مضيئة" },
        { "result_title", "اكتملت المهمة - عادت الإشارة" },
        { "result_stats", "الزمن: {0} ث   الأفضل: {1} ث   المهام المكتملة: {2}" },
        { "play_again", "العب مجددا" },
        { "help_body", "اسحب في الجهة اليسرى للحركة، وفي الجهة اليمنى لتدوير الكاميرا.\n" +
                       "الكشف يضيء خلايا الإرسال الثلاث. اجمعها وفعل البرج ثم اذهب إلى نقطة الإخلاء.\n" +
                       "لوحة المفاتيح: WASD أو الأسهم للحركة، Q للكشف، E للتفاعل، Esc للإيقاف." },
    };

    public static string Get(string language, string key)
    {
        var table = language == "ar" ? Arabic : English;
        string value;
        return table.TryGetValue(key, out value) ? value : key;
    }
}
