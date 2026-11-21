package com.roomrental.webapi.config.auditlog;

import java.util.List;

import org.hibernate.boot.Metadata;
import org.hibernate.boot.spi.BootstrapContext;
import org.hibernate.engine.spi.SessionFactoryImplementor;
import org.hibernate.event.service.spi.EventListenerRegistry;
import org.hibernate.event.spi.EventType;
import org.hibernate.integrator.spi.Integrator;
import org.hibernate.jpa.boot.spi.IntegratorProvider;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.boot.hibernate.autoconfigure.HibernatePropertiesCustomizer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.roomrental.webapi.service.auditlog.AuditEventListener;

@Configuration
@EnableConfigurationProperties(AuditProperties.class)
public class AuditConfiguration {
    @Bean
    HibernatePropertiesCustomizer auditHibernateCustomizer(AuditEventListener listener, AuditProperties settings) {
        return properties -> {
            if (!settings.enabled())
                return;
            Integrator integrator = new Integrator() {
                @Override
                public void integrate(Metadata metadata, BootstrapContext context, SessionFactoryImplementor factory) {
                    EventListenerRegistry registry = factory.getServiceRegistry()
                            .getService(EventListenerRegistry.class);
                    registry.appendListeners(EventType.POST_INSERT, listener);
                    registry.appendListeners(EventType.POST_UPDATE, listener);
                    registry.appendListeners(EventType.POST_DELETE, listener);
                }
            };
            properties.put("hibernate.integrator_provider", (IntegratorProvider) () -> List.of(integrator));
        };
    }
}
