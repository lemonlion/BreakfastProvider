package io.lemonlion.breakfast.web;

import java.util.regex.Pattern;

/** Twin of C# {@code XssValidationExtensions.MustNotContainHtmlOrScript}. */
public final class Xss {

    private static final Pattern HTML_OR_SCRIPT = Pattern.compile("<|>|&lt;|&gt;|script", Pattern.CASE_INSENSITIVE);

    private Xss() {
    }

    public static boolean containsHtmlOrScript(String value) {
        return value != null && HTML_OR_SCRIPT.matcher(value).find();
    }
}
