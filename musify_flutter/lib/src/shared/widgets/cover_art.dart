import 'package:flutter/material.dart';

class CoverArt extends StatelessWidget {
  const CoverArt({
    super.key,
    required this.seed,
    this.icon = Icons.music_note,
    this.size,
    this.borderRadius = 12,
  });

  final String seed;
  final IconData icon;
  final double? size;
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    final hue = (seed.hashCode % 360).abs().toDouble();
    final base = HSLColor.fromAHSL(1, hue, 0.55, 0.45).toColor();
    final accent = HSLColor.fromAHSL(1, (hue + 40) % 360, 0.6, 0.35).toColor();

    return AspectRatio(
      aspectRatio: 1,
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(borderRadius),
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [base, accent],
          ),
        ),
        child: Icon(icon, color: Colors.white.withValues(alpha: 0.85), size: 32),
      ),
    );
  }
}
