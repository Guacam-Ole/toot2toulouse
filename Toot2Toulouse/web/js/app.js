var userSettings;

function fillServerdata() {
    $.getJSON("server", function (data) {
        if (!data.success) {
            errorHandling(data);
            return;
        }

        var lastCategory = undefined;

        var table = "<table><tbody>";
        $.each(data.result, function (key, val) {
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
        if (!data.success) {
            errorHandling(data, $("#error"));
            return;
        }
        $("#disclaimer").text(data.result);
    }, function (error) {
        var e = error;
    });
    return false;
}

function switchAgreement() {
    var accepted = $("#agreement").prop('checked');
    $("#start").prop("disabled", !accepted);
}

function authMastodonStart() {
    hideError();
    var instance = $("#instance").val();
    $.getJSON("mastodon/auth?instance=" + instance, function (data) {
        if (!data.success) {
            errorHandling(data, $("#error"));
            return;
        }
        window.open(data.result);
        $("#mastodoncode").show();
        $("#register").hide();
    });
}

function codeEntered() {
    var code = $("#code").val();
    $("#finishmastodon").prop("disabled", code.length == 0);
}

function authMastodonFinish() {
    hideError();
    var instance = $("#instance").val();
    var code = $("#code").val();

    $.getJSON("mastodon/code?instance=" + instance + "&code=" + code, function (data) {
        if (data.success) {
            hideError();
            $("#mastodoncode").hide();
            $("#twitter").show();
        } else {
            errorHandling(data, $("#error"));
            return;
        }
    });
}

function loadUserSettings() {
    $.getJSON("/user/export", function (data) {
        if (data.error != undefined) {
            errorHandling(data);
        } else {
            displayUserSettings(data);
        }
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
        if (!data.success) saveError(data);
        else saveDelay();
    }, function (error) {
        saveError(error);
    });
}

function saveDelay() {
    $.getJSON("/user/delay", {
        delay: $("#Delay").val()
    }, function (data) {
        if (!data.success) saveError(data);
        else saveSuffix();
    }, function (error) {
        saveError(error);
    });
}


function saveSuffix() {
    $.getJSON("/user/suffix", {
        content: $("#AppSuffixContent").val(),
        hideIfBreaks: $("#AppSuffixHideIfBreaks").prop("checked")
    }, function (data) {
        if (!data.success) saveError(data);
        else saveThread();
    }, function (error) {
        saveError(error);
    });
}


function saveThread() {
    $.getJSON("/user/thread", {
        prefix: $("#LongContentThreadOptionsPrefix").val(),
        suffix: $("#LongContentThreadOptionsSuffix").val()
    }, function (data) {
        if (!data.success) saveError(data);
        else finishSave();
    }, function (error) {
        saveError(error);
    });
}


function finishSave() {
    $("#savestatuscontent").text("Data Saved");
    $("#savestatus").show();
}

function saveError(data) {
    errorHandling(data, $("#savestatuscontent"));
    $("#savestatus").show();
}

function saveSettings() {
    $("#savestatus").hide();
    saveVisibility();
}

/* Global ErrorHandling: */

function hideError() {
    $($("#error").find("p")).text("");
    $("#error").hide();
}

function displayError(code, message, errordiv) {
    if (code == undefined && message == undefined) message = "unexpected error. Something went wrong, sorry";
    $(errordiv.find("p")).text(code + ":" + message);
    errordiv.show();
}

function errorHandling(data, errordiv) {
    var error = data.error.toLowerCase();
    if (errordiv != undefined) {
        displayError(data.error, data.errorMessage, errordiv);
        //$(errordiv.find("p")).text(data.value);
        //errordiv.show();
        return;
    }

    switch (error) {
        case "auth":
            window.location = "/autherror";
            break;
        default:
            window.location = "/error?code=" + error + "&msg=" + data.errorMessage;
            break;
    }
}