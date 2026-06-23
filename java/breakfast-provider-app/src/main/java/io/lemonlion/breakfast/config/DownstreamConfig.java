package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Base URLs for downstream fake services (twin of the C# named HttpClients). */
@ConfigurationProperties(prefix = "downstream")
public class DownstreamConfig {

    private String kitchenServiceUrl = "http://localhost:9081";
    private String cowServiceUrl = "http://localhost:9082";
    private String goatServiceUrl = "http://localhost:9083";
    private String supplierServiceUrl = "http://localhost:9084";

    public String getKitchenServiceUrl() {
        return kitchenServiceUrl;
    }

    public void setKitchenServiceUrl(String kitchenServiceUrl) {
        this.kitchenServiceUrl = kitchenServiceUrl;
    }

    public String getCowServiceUrl() {
        return cowServiceUrl;
    }

    public void setCowServiceUrl(String cowServiceUrl) {
        this.cowServiceUrl = cowServiceUrl;
    }

    public String getGoatServiceUrl() {
        return goatServiceUrl;
    }

    public void setGoatServiceUrl(String goatServiceUrl) {
        this.goatServiceUrl = goatServiceUrl;
    }

    public String getSupplierServiceUrl() {
        return supplierServiceUrl;
    }

    public void setSupplierServiceUrl(String supplierServiceUrl) {
        this.supplierServiceUrl = supplierServiceUrl;
    }
}
