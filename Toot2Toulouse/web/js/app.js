/*const { Callbacks } = require("jquery");*/

var userSettings;

function fillServerdata() {
    $.getJSON("server", function (data) {
        var lastCategory = undefined;

        var table = "<table><tbody>";
        $.each(data, function (key, val) {
            if (lastCategory != val.category) {
                table += "<tr><td><strong>" + val.category + "</strong></td><td></td></tr>";
            }
            lastCategory = val.category;
            table += "<tr><td>" + val.displayName + "</td><td class=''>";
            if (val.displayAsButton) {
                table += "<span class='fake button'>" + val.value + "</span>";
            } else {
                table += val.value;
            }
            table += "</td></tr>";
        });
        table += "</tbody></table>";

        $(table).appendTo("#serverlimitstable");
    }, function (error, whatever) {
        var e = error;
    });
    return false;
}

function readDisclaimer() {
    $.getJSON("disclaimer", function (data) {
        $("#disclaimer").text(data);
    }, function (error, whatever) {
        var e = error;
    });
    return false;
}

function switchAgreement() {
    var accepted = $("#agreement").prop('checked');
    $("#start").prop("disabled", !accepted);
}

function authMastodonStart() {
    var instance = $("#instance").val();
    $.getJSON("mastodon/auth?instance=" + instance, function (data) {
        window.open(data);
    });

    $("#mastodoncode").show();
    $("#register").hide();
}

function codeEntered() {
    var code = $("#code").val();
    $("#finishmastodon").prop("disabled", code.length == 0);
}

function authMastodonFinish() {
    var instance = $("#instance").val();
    var code = $("#code").val();

    $.getJSON("mastodon/code?instance=" + instance + "&code=" + code, function (data) {
        var success = data.key;

        if (success) {
            $("#mastodoncode").hide();
            $("#twitter").show();
        } else {
            $($("#error").find("p")).text(data.value);
            $("#error").show();
        }
    });
}

function loadUserSettings() {
    $.getJSON("/user/export", function (data) {
        if (data.error == "auth") {
            window.location = "/autherror";
            return;
        }
        displayUserSettings(data);
    });
}


function styleButtonByValue(chkBox) {
    var button = chkBox.prev();
    value = chkBox.prop("checked");
    if (value) {
        button.attr("class", "button button-primary");
    } else {
        button.attr("class", "button");
    }
}



function displayUserSettings(user) {
    userSettings = user.config;
    $("#name").text(user.mastodon.displayName);
    $("#VisibilitiesTootPublic").prop("checked", userSettings.visibilitiesToPost.indexOf("Public") >= 0);
    $("#VisibilitiesTootUnlisted").prop("checked", userSettings.visibilitiesToPost.indexOf("Unlisted") >= 0);
    $("#VisibilitiesTootPrivate").prop("checked", userSettings.visibilitiesToPost.indexOf("Private") >= 0);

    $("#Delay").val(userSettings.delay);
    $("#AppSuffixContent").val(userSettings.appSuffix.content);
    $("#AppSuffixHideIfBreaks").prop("checked", userSettings.appSuffix.hideIfBreaks);
    $("#LongContentThreadOptionsPrefix").val(userSettings.longContentThreadOptions.prefix);
    $("#LongContentThreadOptionsSuffix").val(userSettings.longContentThreadOptions.suffix);



    styleButtonByValue($("#VisibilitiesTootPublic"));
    styleButtonByValue($("#VisibilitiesTootUnlisted"));
    styleButtonByValue($("#VisibilitiesTootPrivate"));
    styleButtonByValue($("#AppSuffixHideIfBreaks"));

    $("#savestatus").hide();
}

function saveVisibility() {
    $.getJSON("/user/visibility", {
        publicToots: ($("#VisibilitiesTootPublic").prop("checked")),
        notListedToots: ($("#VisibilitiesTootUnlisted").prop("checked")),
        privateToots: ($("#VisibilitiesTootPrivate").prop("checked"))
    }, function (data) {
        if (!data.success) saveError();
        else saveDelay();
    });
}

function saveDelay() {

    $.getJSON("/user/delay", {
        delay: $("#Delay").val()
    }, function (data) {
        if (!data.success) saveError();
        else saveSuffix();
    });
}

function saveSuffix() {
    $.getJSON("/user/suffix", {
        content: $("#AppSuffixContent").val(),
        hideIfBreaks: $("#AppSuffixHideIfBreaks").prop("checked")
    }, function (data) {
        if (!data.success) saveError();
        else saveThread();
    });
}

function saveThread() {

    $.getJSON("/user/thread", {
        prefix: $("#LongContentThreadOptionsPrefix").val(),
        suffix: $("#LongContentThreadOptionsSuffix").val()
    }, function (data) {
        if (!data.success) saveError();
        else finishSave();
    });
}

function finishSave() {
    $("#savestatuscontent").text("Data Saved");
    $("#savestatus").show();
}

function saveError() {
    $("#savestatuscontent").text("Error on saving");
    $("#savestatus").show();
}

function saveSettings() {
    $("#savestatus").hide();
    saveVisibility();
}



