/// <reference path="typings/jquery/jquery.d.ts" />
/// <reference path="typings/signalr/signalr.d.ts" />

interface BuildProgressHubServer {
    register(service: string, user: string, repo: string, sha: string): JQueryPromise<void>
}

interface BuildProgressHubClient {
    onTextChanged: (text: string) => void;
    onProgressChanged: (text: string, current: number, maximum: number) => void;
    onCompleted: () => void;
}

function startReceiving(service: string, user: string, repo: string, sha: string) {
    var $text = $("#text");
    var $progressbar = $("#progressbar");

    var hub = (<any>$.connection).buildProgressHub;
    var client = <BuildProgressHubClient>hub.client;
    var server = <BuildProgressHubServer>hub.server;

    client.onTextChanged = text => {
        $text.text(text);
    };

    var isFirst = true;
    function firstHandler() {
        if (isFirst) {
            isFirst = false;
            $progressbar.removeClass("progress-bar-striped")
                .removeClass("active");
        }
    };

    client.onProgressChanged = (text, current, maximum) => {
        firstHandler();
        $text.text(text);
        $progressbar.text(current + " / " + maximum)
            .css("width", (current / maximum * 100) + "%");
    };

    client.onCompleted = () => {
        firstHandler();
        $progressbar.text("").css("width", "100%");
        $text.text("Loading...");
        location.reload();
    };

    $.connection.hub.start()
        .done(() => server.register(service, user, repo, sha));
}
