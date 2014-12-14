/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/signalr/signalr.d.ts" />
function startReceiving(service, user, repo, sha) {
    var $text = $("#text");
    var $progressbar = $("#progressbar");
    var hub = $.connection.buildProgressHub;
    var client = hub.client;
    var server = hub.server;
    client.onTextChanged = function (text) {
        $text.text(text);
    };
    var isFirst = true;
    function firstHandler() {
        if (isFirst) {
            isFirst = false;
            $progressbar.removeClass("progress-bar-striped").removeClass("active");
        }
    }
    ;
    client.onProgressChanged = function (text, current, maximum) {
        firstHandler();
        $text.text(text);
        $progressbar.text(current + " / " + maximum).css("width", (current / maximum * 100) + "%");
    };
    client.onCompleted = function () {
        firstHandler();
        $progressbar.text("").css("width", "100%");
        $text.text("Loading...");
        location.reload();
    };
    $.connection.hub.start().done(function () { return server.register(service, user, repo, sha); });
}
//# sourceMappingURL=Pending.js.map