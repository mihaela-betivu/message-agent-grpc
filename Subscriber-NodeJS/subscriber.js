const grpc = require('@grpc/grpc-js');
const protoLoader = require('@grpc/proto-loader');
const path = require('path');

// Configuration
const BROKER_ADDRESS = 'http://127.0.0.1:5001';
const SUBSCRIBER_PORT = process.argv[3] ? parseInt(process.argv[3]) : 3001; // Port from command line or default
const SUBSCRIBER_ADDRESS = `http://localhost:${SUBSCRIBER_PORT}`;

// Load proto files
const packageDefinition = protoLoader.loadSync([
  path.join(__dirname, 'protos/subscribe.proto'),
  path.join(__dirname, 'protos/notify.proto')
], {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
  oneofs: true
});

const grpcPackage = grpc.loadPackageDefinition(packageDefinition).GrpcAgent;

// Store subscribed topics
const subscribedTopics = new Set();

// Track which topics we're subscribed to for display purposes
let subscribedTopicsList = [];

// gRPC Notifier service implementation
const notifierService = {
  Notify: (call, callback) => {
    const { content, topic } = call.request;
    
    // Use the topic from the request if available, otherwise try to extract from content
    let topicInfo = '';
    let cleanContent = content;
    
    if (topic && topic.trim()) {
      // Use the topic field from the request
      topicInfo = ` [TOPIC: ${topic}]`;
    } else {
      // Fallback: Try to extract topic from content if it follows a pattern like [TOPIC:topic_name]
      const topicMatch = content.match(/\[TOPIC:([^\]]+)\]/);
      if (topicMatch) {
        topicInfo = ` [TOPIC: ${topicMatch[1]}]`;
        // Remove the topic tag from content for cleaner display
        cleanContent = content.replace(/\[TOPIC:[^\]]+\]\s*/, '');
      } else {
        // If no topic tag in message, try to detect topic from content patterns
        let detectedTopic = '';
        
        // Check if content starts with topic name (case insensitive)
        for (const subscribedTopic of subscribedTopicsList) {
          const topicPattern = new RegExp(`^${subscribedTopic}\\s*[:\\-]`, 'i');
          if (topicPattern.test(content)) {
            detectedTopic = subscribedTopic;
            cleanContent = content.replace(topicPattern, '').trim();
            break;
          }
        }
        
        if (detectedTopic) {
          topicInfo = ` [TOPIC: ${detectedTopic}]`;
        } else {
          // If no topic detected, show all subscribed topics for reference
          if (subscribedTopicsList.length > 1) {
            topicInfo = ` [FROM: ${subscribedTopicsList.join(', ')}]`;
          } else if (subscribedTopicsList.length === 1) {
            topicInfo = ` [FROM: ${subscribedTopicsList[0]}]`;
          }
        }
      }
    }
    
    // Get current timestamp for better logging
    const timestamp = new Date().toLocaleTimeString();
    
    // Check if it's a historical message
    if (content.includes('[HISTORICAL]')) {
      // Extract original timestamp from historical message (format MM:SS)
      const timestampMatch = cleanContent.match(/\(sent at (\d{2}:\d{2})\)/);
      const originalTime = timestampMatch ? timestampMatch[1] : timestamp;
      
      // Clean content by removing historical markers and timestamps
      let displayContent = cleanContent
        .replace(/\[HISTORICAL\]\s*/, '') // Remove [HISTORICAL] marker
        .replace(/\s*\(sent at \d{2}:\d{2}\)/, ''); // Remove (sent at 47:58)
      
      console.log(`📜 [${originalTime}] [${topic}] ${displayContent}`);
    } else {
      console.log(`📨 [${timestamp}] [${topic}] ${cleanContent}`);
    }
    
    callback(null, { isSuccess: true });
  }
};

// Create gRPC server
const server = new grpc.Server();
server.addService(grpcPackage.Notifier.service, notifierService);

// Start gRPC server
server.bindAsync(`0.0.0.0:${SUBSCRIBER_PORT}`, grpc.ServerCredentials.createInsecure(), (err, port) => {
  if (err) {
    console.error('❌ Failed to start gRPC server:', err);
    process.exit(1);
  }
  console.log(`🚀 [PORT ${SUBSCRIBER_PORT}] Subscriber gRPC server running on port ${port}`);
});

// gRPC client for subscribing
const subscriberClient = new grpcPackage.Subscriber(
  BROKER_ADDRESS.replace('http://', ''),
  grpc.credentials.createInsecure()
);

// Function to subscribe to a topic
function subscribeToTopic(topic) {
  return new Promise((resolve, reject) => {
    const request = {
      topic: topic.toLowerCase(),
      address: SUBSCRIBER_ADDRESS
    };

    console.log(`🔔 [PORT ${SUBSCRIBER_PORT}] Subscribing to topic: ${topic}`);
    
    subscriberClient.Subscribe(request, (error, response) => {
      if (error) {
        console.error(`❌ [PORT ${SUBSCRIBER_PORT}] Error subscribing to topic ${topic}:`, error.message);
        reject(error);
      } else {
        console.log(`✅ [PORT ${SUBSCRIBER_PORT}] Successfully subscribed to topic: ${topic}`);
        subscribedTopics.add(topic);
        resolve(response);
      }
    });
  });
}

// Main function
async function main() {
  console.log('📡 Message Agent Subscriber (Node.js)');
  console.log(`Broker: ${BROKER_ADDRESS}`);
  console.log(`Subscriber: ${SUBSCRIBER_ADDRESS}`);
  console.log('');

  // Get topics from command line or use default
  // Support multiple topics separated by comma: node subscriber.js "news,sports,weather" 3001
  // Support topic detection mode: node subscriber.js "news,sports,weather" 3001 --detect-topics
  const topicsInput = process.argv[2] || 'news';
  const port = process.argv[3] || '3001';
  const detectTopics = process.argv.includes('--detect-topics');
  
  // Parse topics (split by comma and trim whitespace)
  const topics = topicsInput.split(',').map(topic => topic.trim()).filter(topic => topic.length > 0);

  console.log(`Topics: ${topics.join(', ')}`);
  console.log(`Port: ${port}`);
  if (detectTopics) {
    console.log(`🔍 Topic detection mode: ENABLED`);
  }

  try {
    // Store the topics list for display purposes
    subscribedTopicsList = [...topics];
    
    // Subscribe to all topics
    const subscriptionPromises = topics.map(topic => subscribeToTopic(topic));
    await Promise.all(subscriptionPromises);
    
    console.log('');
    console.log(`🎯 [PORT ${SUBSCRIBER_PORT}] Subscriber is ready! Listening to ${topics.length} topic(s):`);
    topics.forEach((topic, index) => {
      console.log(`   ${index + 1}. ${topic}`);
    });
    console.log('');
    console.log('Press Ctrl+C to exit');
    console.log('');
    
  } catch (error) {
    console.error(`❌ [PORT ${SUBSCRIBER_PORT}] Failed to start subscriber:`, error.message);
    process.exit(1);
  }
}

// Graceful shutdown
process.on('SIGINT', () => {
  console.log(`\n👋 [PORT ${SUBSCRIBER_PORT}] Shutting down subscriber...`);
  server.forceShutdown();
  console.log(`✅ [PORT ${SUBSCRIBER_PORT}] Subscriber stopped`);
  process.exit(0);
});

// Start the subscriber
main().catch(console.error);
