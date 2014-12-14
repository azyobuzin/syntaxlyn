/// <reference path="typings/jquery/jquery.d.ts" />
(function () {
    var right = document.getElementById("right");
    var rightHeader = document.getElementById("right-header");
    var sourceStyle = document.getElementById("source").style;
    var handler = function () {
        rightHeader.style.width = right.clientWidth + "px";
        sourceStyle.marginTop = rightHeader.clientHeight + "px";
    };
    window.addEventListener("resize", handler);
    handler();
})();
//# sourceMappingURL=SourceView.js.map