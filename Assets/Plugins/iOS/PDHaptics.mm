// Taptic feedback bridge for HapticService (iOS). Style ids match PixelDefense.Services.Haptics.HapticStyle.
#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *pdLight;
static UIImpactFeedbackGenerator *pdMedium;
static UIImpactFeedbackGenerator *pdHeavy;
static UISelectionFeedbackGenerator *pdSelection;
static UINotificationFeedbackGenerator *pdNotification;

static void PDHapticsEnsure(void)
{
    if (pdLight != nil) return;
    pdLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    pdMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    pdHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
    pdSelection = [[UISelectionFeedbackGenerator alloc] init];
    pdNotification = [[UINotificationFeedbackGenerator alloc] init];
    [pdLight prepare];
    [pdMedium prepare];
}

extern "C" void PDHaptics_Play(int style)
{
    PDHapticsEnsure();
    switch (style)
    {
        case 0: [pdSelection selectionChanged]; break;
        case 1: [pdLight impactOccurred]; [pdLight prepare]; break;
        case 2: [pdMedium impactOccurred]; [pdMedium prepare]; break;
        case 3: [pdHeavy impactOccurred]; break;
        case 4: [pdNotification notificationOccurred:UINotificationFeedbackTypeSuccess]; break;
        case 5: [pdNotification notificationOccurred:UINotificationFeedbackTypeError]; break;
        default: break;
    }
}
