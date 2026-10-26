import 'package:flutter/material.dart';

import '../design.dart';

class PrivacyPolicyScreen extends StatelessWidget {
  const PrivacyPolicyScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: ScribeColors.canvas,
        elevation: 0,
        scrolledUnderElevation: 0,
        title: const Text('Privacy policy'),
      ),
      body: ScribeSheet(
        children: const [
          Text('Effective date: 22 October 2026', style: ScribeType.metadata),
          Text(
            'RackPeek for iOS connects to a RackPeek server that you choose and operate.',
            style: ScribeType.body,
          ),
          _PolicySection(
            title: 'Information the app uses',
            body:
                'The server address and API key you enter are saved in secure storage on your device. The app sends the API key with requests to that server. Inventory is received from the server and shown in the app; changes you make are sent back to it. The RackPeek app publisher does not receive this information through an app-operated service. Your server operator may keep its own logs and inventory.',
          ),
          _PolicySection(
            title: 'Network security',
            body:
                'Use HTTPS when your server supports it. With unencrypted HTTP, your API key and inventory traffic may be visible to other systems on the network. Only connect to a server you trust.',
          ),
          _PolicySection(
            title: 'Analytics and tracking',
            body:
                'The current app does not include advertising, analytics, or cross-app tracking features.',
          ),
          _PolicySection(
            title: 'Your choices',
            body:
                'Choose Disconnect in settings to remove the saved server address and API key from this device. This does not delete information from your RackPeek server.',
          ),
          _PolicySection(
            title: "Children's privacy",
            body:
                'RackPeek is a general-purpose infrastructure inventory tool and is not designed for children.',
          ),
          _PolicySection(
            title: 'Contact',
            body:
                'For privacy questions, contact the app publisher through the project support page at github.com/Timmoth/RackPeek/issues.',
          ),
        ],
      ),
    );
  }
}

class _PolicySection extends StatelessWidget {
  const _PolicySection({required this.title, required this.body});

  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: ScribeType.sectionLabel),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Text(body, style: ScribeType.body),
        ],
      ),
    );
  }
}
