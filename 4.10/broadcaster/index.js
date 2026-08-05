const { connect, StringCodec } = require('nats');

const natsUrl = process.env.NATS_URL || 'nats://my-nats:4222';
const webhookUrl = process.env.WEBHOOK_URL || process.env.DISCORD_WEBHOOK_URL || '';
const podName = process.env.HOSTNAME || 'broadcaster-pod';
const sc = StringCodec();

async function postWebhook(textMessage) {
  const payload = {
    user: 'bot',
    message: textMessage,
  };

  if (!webhookUrl) {
    console.log(`[INFO] [broadcaster] [${podName}] WEBHOOK_URL not configured. Payload:`, JSON.stringify(payload));
    return;
  }

  try {
    const response = await fetch(webhookUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    console.log(`[INFO] [broadcaster] [${podName}] Posted message to webhook. Status: ${response.status}`);
  } catch (postErr) {
    console.error(`[ERROR] [broadcaster] [${podName}] Failed to post to webhook:`, postErr.message);
  }
}

async function main() {
  console.log(`[INFO] [broadcaster] Connecting to NATS at ${natsUrl}...`);
  const nc = await connect({ servers: natsUrl });
  console.log(`[INFO] [broadcaster] Connected to NATS at ${natsUrl}`);

  // Queue group: only one of N replicas handles each message (safe to scale to 6+).
  const sub = nc.subscribe('todo_events', { queue: 'broadcaster-group' });
  console.log(`[INFO] [broadcaster] Subscribed to 'todo_events' with queue group 'broadcaster-group'`);

  const shutdown = async (signal) => {
    console.log(`[INFO] [broadcaster] ${signal} received, draining NATS subscription...`);
    try {
      await sub.drain();
      await nc.drain();
    } catch (err) {
      console.error(`[ERROR] [broadcaster] Error during drain:`, err.message);
    }
    process.exit(0);
  };
  process.on('SIGTERM', () => shutdown('SIGTERM'));
  process.on('SIGINT', () => shutdown('SIGINT'));

  for await (const msg of sub) {
    const textMessage = sc.decode(msg.data);
    console.log(`[INFO] [broadcaster] [${podName}] Received message: '${textMessage}'`);
    await postWebhook(textMessage);
  }
}

main().catch((err) => {
  console.error('[FATAL] Unhandled error in main loop:', err);
  process.exit(1);
});
