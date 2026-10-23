import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

/// Semantic color tokens from DESIGN.md (light appearance).
class ScribeColors {
  static const Color canvas = Color(0xFFF2F2F7);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color primaryText = Color(0xFF1C1C1E);
  static const Color secondaryText = Color(0xFF63636B);
  static const Color tertiaryText = Color(0xFF74747C);
  static const Color accent = Color(0xFF0066D6);
  static const Color accentText = Color(0xFF005BC4);
  static const Color accentSoft = Color(0xFFE8F1FF);
  static const Color separator = Color(0xFFE5E5EA);
  static const Color strongSeparator = Color(0xFFC7C7CC);
  static const Color success = Color(0xFF248A3D);
  static const Color danger = Color(0xFFC9342D);
  static const Color warningSurface = Color(0xFFFFEBE9);
  static const Color infoSurface = Color(0xFFE8F1FF);
}

/// Layout constants from DESIGN.md.
class ScribeSpacing {
  static const double screenMargin = 20;
  static const double panelPadding = 16;
  static const double majorSectionGap = 26;
  static const double relatedGap = 10;
  static const double panelRadius = 20;
  static const double controlRadius = 12;
  static const double minTarget = 44;
}

/// Type scale from DESIGN.md. Platform sans-serif; never clamp text scale.
class ScribeType {
  static const TextStyle hero = TextStyle(
      fontSize: 34, fontWeight: FontWeight.bold, color: ScribeColors.primaryText);
  static const TextStyle section = TextStyle(
      fontSize: 28,
      fontWeight: FontWeight.w600,
      color: ScribeColors.primaryText);
  static const TextStyle wordmark = TextStyle(
      fontSize: 22, fontWeight: FontWeight.bold, color: ScribeColors.primaryText);
  static const TextStyle title = TextStyle(
      fontSize: 17,
      fontWeight: FontWeight.w600,
      color: ScribeColors.primaryText);
  static const TextStyle body =
      TextStyle(fontSize: 16, color: ScribeColors.primaryText);
  static const TextStyle metadata =
      TextStyle(fontSize: 13, color: ScribeColors.secondaryText);
  static const TextStyle sectionLabel = TextStyle(
      fontSize: 12, color: ScribeColors.tertiaryText, height: 1.1);
}

ThemeData scribeTheme() {
  final base = ThemeData(
    brightness: Brightness.light,
    useMaterial3: true,
  );
  return base.copyWith(
    scaffoldBackgroundColor: ScribeColors.canvas,
    colorScheme: const ColorScheme.light(
      primary: ScribeColors.accent,
      secondary: ScribeColors.accent,
      surface: ScribeColors.surface,
      error: ScribeColors.danger,
    ),
    textTheme: base.textTheme.apply(
      bodyColor: ScribeColors.primaryText,
      displayColor: ScribeColors.primaryText,
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: ScribeColors.surface,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
        borderSide: const BorderSide(color: ScribeColors.strongSeparator),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
        borderSide: const BorderSide(color: ScribeColors.strongSeparator),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
        borderSide: const BorderSide(color: ScribeColors.accent, width: 2),
      ),
    ),
    snackBarTheme: const SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      backgroundColor: ScribeColors.primaryText,
      contentTextStyle: TextStyle(color: Colors.white),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: ScribeColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ScribeSpacing.panelRadius),
      ),
    ),
    bottomSheetTheme: const BottomSheetThemeData(
      backgroundColor: ScribeColors.canvas,
    ),
  );
}

/// Opaque rounded surface, clipped to its bounds.
class SurfacePanel extends StatelessWidget {
  const SurfacePanel({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(ScribeSpacing.panelPadding),
  });

  final Widget child;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: ScribeColors.surface,
        borderRadius: BorderRadius.circular(ScribeSpacing.panelRadius),
      ),
      clipBehavior: Clip.antiAlias,
      padding: padding,
      child: child,
    );
  }
}

/// Filled primary action or tinted secondary action. 44pt minimum target.
class ScribeAction extends StatelessWidget {
  const ScribeAction({
    super.key,
    required this.label,
    this.onPressed,
    this.secondary = false,
    this.danger = false,
    this.busy = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool secondary;
  final bool danger;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final Color fill = danger
        ? ScribeColors.danger
        : secondary
            ? ScribeColors.accentSoft
            : ScribeColors.accent;
    final Color foreground =
        danger ? Colors.white : secondary ? ScribeColors.accentText : Colors.white;

    return SizedBox(
      height: ScribeSpacing.minTarget,
      child: FilledButton(
        style: FilledButton.styleFrom(
          backgroundColor: fill,
          foregroundColor: foreground,
          disabledBackgroundColor: fill.withValues(alpha: 0.4),
          disabledForegroundColor: foreground.withValues(alpha: 0.6),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
          ),
        ),
        onPressed: onPressed == null || busy ? null : onPressed,
        child: busy
            ? SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(
                    strokeWidth: 2, color: foreground))
            : Text(label, style: ScribeType.title),
      ),
    );
  }
}

/// Selectable capsule with selected semantics and a 44pt target.
class FilterPill extends StatelessWidget {
  const FilterPill({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      selected: selected,
      button: true,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(ScribeSpacing.minTarget / 2),
        child: Container(
          height: ScribeSpacing.minTarget - 8,
          alignment: Alignment.center,
          padding: const EdgeInsets.symmetric(horizontal: 16),
          decoration: BoxDecoration(
            color: selected ? ScribeColors.accent : ScribeColors.surface,
            borderRadius:
                BorderRadius.circular(ScribeSpacing.minTarget / 2),
            border: Border.all(
              color: selected ? ScribeColors.accent : ScribeColors.strongSeparator,
            ),
          ),
          child: Text(
            label,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
              color: selected ? Colors.white : ScribeColors.primaryText,
            ),
          ),
        ),
      ),
    );
  }
}

/// Sentence-case metadata on a quiet neutral fill, with wrapping.
class NoteFact extends StatelessWidget {
  const NoteFact({super.key, required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: ScribeColors.canvas,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(text, style: ScribeType.metadata),
    );
  }
}

/// An icon and explanation for information or errors.
class InlineNotice extends StatelessWidget {
  const InlineNotice({
    super.key,
    required this.message,
    this.isError = false,
    this.onRetry,
  });

  final String message;
  final bool isError;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(ScribeSpacing.panelPadding),
      decoration: BoxDecoration(
        color: isError ? ScribeColors.warningSurface : ScribeColors.infoSurface,
        borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            isError ? CupertinoIcons.exclamationmark_triangle : CupertinoIcons.info,
            size: 20,
            color: isError ? ScribeColors.danger : ScribeColors.accentText,
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(message, style: ScribeType.body),
          ),
          if (onRetry != null)
            TextButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    );
  }
}

/// A familiar glyph in a tinted 40-point tile.
class CategoryMark extends StatelessWidget {
  const CategoryMark({
    super.key,
    required this.icon,
    this.tint = ScribeColors.accent,
    this.soft = ScribeColors.accentSoft,
  });

  final IconData icon;
  final Color tint;
  final Color soft;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 40,
      height: 40,
      decoration: BoxDecoration(
        color: soft,
        borderRadius: BorderRadius.circular(10),
      ),
      alignment: Alignment.center,
      child: Icon(icon, size: 20, color: tint),
    );
  }
}

/// Readable sentence-case status with a dot.
class StatusTag extends StatelessWidget {
  const StatusTag({
    super.key,
    required this.label,
    required this.color,
  });

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(label, style: ScribeType.metadata),
      ],
    );
  }
}

/// Restrained section hierarchy text.
class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(text.toUpperCase(),
        style: ScribeType.sectionLabel.copyWith(letterSpacing: 0.5));
  }
}

/// App wordmark.
class Wordmark extends StatelessWidget {
  const Wordmark({super.key});

  @override
  Widget build(BuildContext context) {
    return const Text('RackPeek', style: ScribeType.wordmark);
  }
}

/// Cupertino activity indicator.
class ScribeSpinner extends StatelessWidget {
  const ScribeSpinner({super.key});

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(ScribeSpacing.majorSectionGap),
        child: CupertinoActivityIndicator(radius: 14),
      ),
    );
  }
}

/// Scrollable, safe-area-aware column with screen margins.
class ScribeSheet extends StatelessWidget {
  const ScribeSheet({
    super.key,
    required this.children,
    this.scrollable = true,
  });

  final List<Widget> children;
  final bool scrollable;

  @override
  Widget build(BuildContext context) {
    final Column content = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: _spaced(children),
    );

    if (!scrollable) {
      return SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(
              horizontal: ScribeSpacing.screenMargin),
          child: content,
        ),
      );
    }

    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(
            horizontal: ScribeSpacing.screenMargin, vertical: 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: _spaced(children),
        ),
      ),
    );
  }

  static List<Widget> _spaced(List<Widget> children) {
    final List<Widget> result = [];
    for (var i = 0; i < children.length; i++) {
      result.add(children[i]);
      if (i < children.length - 1) {
        result.add(const SizedBox(height: ScribeSpacing.majorSectionGap));
      }
    }
    return result;
  }
}
