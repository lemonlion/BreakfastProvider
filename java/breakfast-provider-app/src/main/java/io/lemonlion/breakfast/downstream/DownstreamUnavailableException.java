package io.lemonlion.breakfast.downstream;

/** Raised when a downstream service (Cow/Goat) is unreachable or returns an error → mapped to HTTP 502. */
public class DownstreamUnavailableException extends RuntimeException {

    private final String serviceName;

    public DownstreamUnavailableException(String serviceName, String message) {
        super(message);
        this.serviceName = serviceName;
    }

    public String getServiceName() {
        return serviceName;
    }
}
