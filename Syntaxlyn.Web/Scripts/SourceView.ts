/// <reference path="typings/jquery/jquery.d.ts" />

(() => {
    var right = document.getElementById("right");
    var rightHeader = document.getElementById("right-header");
    var sourceStyle = document.getElementById("source").style;
    var handler = () => {
        rightHeader.style.width = right.clientWidth + "px";
        sourceStyle.marginTop = rightHeader.clientHeight + "px";
    };
    window.addEventListener("resize", handler);
    handler();
})();
