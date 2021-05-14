"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var mqtt_1 = require("mqtt");
var client = mqtt_1.connect('ws://' + location.host + '/mqtt', {
    //clientId: "client" + Math.floor(Math.random() * 6) + 1
    clientId: "browser-" + guid(),
    username: 'browser',
    password: 'password',
    clean: true
});
window.onbeforeunload = function () {
    client.end();
};
var publishButton = document.getElementById("publish");
var topicInput = document.getElementById("topic");
var msgInput = document.getElementById("msg");
var stateParagraph = document.getElementById("state");
var msgsList = document.getElementById("msgs");
publishButton.onclick = function (click) {
    var topic = topicInput.value;
    var msg = msgInput.value;
    client.publish(topic, msg);
};
client.on('connect', function () {
    client.subscribe('browser/#', { qos: 0 }, function (err, granted) {
        console.log(err);
    });
    //client.publish('presence', 'Hello mqtt');
    stateParagraph.innerText = "connected";
    showMsg("[connect]");
});
client.on("error", function (e) {
    showMsg("error: " + e.message);
});
client.on("reconnect", function () {
    stateParagraph.innerText = "reconnecting";
    showMsg("[reconnect]");
});
client.on('message', function (topic, message) {
    showMsg(topic + ": " + message.toString());
});
function showMsg(msg) {
    //console.log(msg);
    var node = document.createElement("LI");
    node.appendChild(document.createTextNode(msg));
    msgsList.appendChild(node);
    if (msgsList.childElementCount > 50) {
        msgsList.removeChild(msgsList.childNodes[0]);
    }
}
/**
 *获取id
 */
function guid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}
//# sourceMappingURL=app.js.map