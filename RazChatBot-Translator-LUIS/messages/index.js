/*-----------------------------------------------------------------------------
This template demonstrates how to use an IntentDialog with a LuisRecognizer to add 
natural language support to a bot. 
For a complete walkthrough of creating this type of bot see the article at
https://aka.ms/abs-node-luis
-----------------------------------------------------------------------------*/
"use strict";
var builder = require("botbuilder");
var botbuilder_azure = require("botbuilder-azure");
var path = require('path');
var request = require('request');
var parseString = require('xml2js').parseString;
var urlencode = require('urlencode');

var useEmulator = process.env.BotEnv == null ? true : (process.env.BotEnv == 'local');
console.log('index.js useEmulator: ' + useEmulator);

var connector = useEmulator ? new builder.ChatConnector() : new botbuilder_azure.BotServiceConnector({
    appId: process.env['MicrosoftAppId'],
    appPassword: process.env['MicrosoftAppPassword'],
    stateEndpoint: process.env['BotStateEndpoint'],
    openIdMetadata: process.env['BotOpenIdMetadata']
});

var bot = new builder.UniversalBot(connector);
bot.localePath(path.join(__dirname, './locale'));

//=========================================================
// Bot Translation Middleware
//=========================================================
console.log("Initializing Translator API");
var TranslationAPIKey = useEmulator ? <YOUR_AZURE_COGNITIVE_BING_TRANSLATOR_API_KEY_HERE> : process.env.TranslationAPIKey;

var tokenHandler = require('./tokenHandler');

// Start generating tokens needed to use the translator API
tokenHandler.init();

// Can hardcode if you know that the language coming in will be chinese/english for sure
// Otherwise can use the code for locale detection provided here: https://docs.botframework.com/en-us/node/builder/chat/localization/#navtitle
var FROMLOCALE = 'fil'; // Filipino locale
var TOLOCALE = 'en'; // English locale

// Documentation for text translation API here: http://docs.microsofttranslator.com/text-translate.html
bot.use({
    receive: function(event, next) {
        var token = tokenHandler.token();
        if (token && token !== "") { //not null or empty string
            var urlencodedtext = urlencode(event.text); // convert foreign characters to utf8
            var options = {
                method: 'GET',
                url: 'http://api.microsofttranslator.com/v2/Http.svc/Translate' + '?text=' + urlencodedtext + '&from=' + FROMLOCALE + '&to=' + TOLOCALE,
                headers: {
                    'Authorization': 'Bearer ' + token
                }
            };

            request(options, function(error, response, body) {
                //Check for error
                if (error) {
                    return console.log('Error:', error);
                } else if (response.statusCode !== 200) {
                    return console.log('Invalid Status Code Returned:', response.statusCode);
                } else {
                    console.log('Filipino:' + event.text);

                    // Returns in xml format, no json option :(
                    parseString(body, function(err, result) {
                        event.text = result.string._;
                        console.log('English:' + event.text);
                        next();
                    });
                }
            });
        } else {
            console.log("No token");
            next();
        }
    }
});


//=========================================================
// Bots Dialogs
//=========================================================
console.log("Initializing LUIS");

var luisAppId = useEmulator ? <YOUR_LUIS_APP_ID_HERE> : process.env.LuisAppId;
var luisAPIKey = useEmulator ? <YOUR_AZURE_LUIS_API_KEY_HERE> : process.env.LuisAPIKey;
var luisAPIHostName = useEmulator ? 'southeastasia.api.cognitive.microsoft.com' : process.env.LuisAPIHostName;

const LuisModelUrl = 'https://' + luisAPIHostName + '/luis/v2.0/apps/' + luisAppId + '?subscription-key=' + luisAPIKey + '&timezoneOffset=480&verbose=true';

// Main dialog with LUIS
var recognizer = new builder.LuisRecognizer(LuisModelUrl);
var intents = new builder.IntentDialog({ recognizers: [recognizer] })
    .matches('Greetings', (session, args) => {
        session.send('Kamusta? Ako si Omar, isang Azure Bot na iniintindi ka gamit ang Azure Translator API.\nPano kita matutulungan ngayon?\n\n(DEBUG: Greetings Intent. Message:\'%s\')', session.message.text);
    })
    .matches('Help', (session, args) => {
        session.send('Ang intensyon mo ay maghanap humingi ng tulong.\n\n(DEBUG: Help Intent. Message:\'%s\')', session.message.text);
    })
    .matches('Goodbyes', (session, args) => {
        session.send('Paalam po.\n\n(DEBUG: Goodbyes Intent. Message:\'%s\')', session.message.text);
    })
    .matches('Entertainment.Search', (session, args) => {
        session.send('Ang intensyon mo ay maghanap ng detalye tungkol sa palabas.\n\n(DEBUG: Entertainment.Search Intent. Message:\'%s\')', session.message.text);
    })
    .onDefault((session) => {
        session.send('Paunawa, hindi ko na intindihan ang intensyon.\n\n(DEBUG: None Intent. Message:\'%s\')', session.message.text);
    });

bot.dialog('/', intents);

if (useEmulator) {
    console.log("Use Emulator");

    var restify = require('restify');
    var server = restify.createServer();
    server.listen(3978, function() {
        console.log('Local Bot (DEBUG) End Point at http://localhost:3978/api/messages');
    });
    server.post('/api/messages', connector.listen());
} else {
    module.exports = { default: connector.listen() }
}