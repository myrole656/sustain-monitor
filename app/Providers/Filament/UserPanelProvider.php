<?php

namespace App\Providers\Filament;

use Filament\Http\Middleware\Authenticate;
use Filament\Http\Middleware\AuthenticateSession;
use Filament\Http\Middleware\DisableBladeIconComponents;
use Filament\Http\Middleware\DispatchServingFilamentEvent;
use Filament\Panel;
use Filament\PanelProvider;
use Filament\Support\Colors\Color;
use App\Filament\Resources\Projects\ProjectResource;
use App\Filament\User\Pages\Dashboard;
use Illuminate\Cookie\Middleware\AddQueuedCookiesToResponse;
use Illuminate\Cookie\Middleware\EncryptCookies;
use Illuminate\Foundation\Http\Middleware\VerifyCsrfToken;
use Illuminate\Routing\Middleware\SubstituteBindings;
use Illuminate\Session\Middleware\StartSession;
use Illuminate\View\Middleware\ShareErrorsFromSession;

class UserPanelProvider extends PanelProvider
{
    public function panel(Panel $panel): Panel
    {
        return $panel
            ->default()
            ->id('user')
            ->path('user')

            // ✅ Set your custom panel colors here
            ->colors([
                'primary' => Color::Amber,
                'success' => Color::Green,
                'danger'  => Color::Red,
                'warning' => Color::Amber,
                'info'    => Color::Blue,
                'gray'    => Color::Zinc,
            ])

            // Resources available in this panel
            ->resources([
                ProjectResource::class,
            ])

            // Discover pages automatically
            ->discoverPages(
                in: app_path('Filament/Pages'),
                for: 'App\\User\\Filament\\Pages'
            )

            // Discover widgets automatically
            ->discoverWidgets(
                in: app_path('Filament/User/Widgets'),
                for: 'App\\Filament\\User\\Widgets'
            )

            // Explicit pages
            ->pages([
                Dashboard::class,
                \App\Filament\User\Pages\ProjectStatus::class,
            ])

            // Middleware
            ->middleware([
                EncryptCookies::class,
                AddQueuedCookiesToResponse::class,
                StartSession::class,
                AuthenticateSession::class,
                ShareErrorsFromSession::class,
                VerifyCsrfToken::class,
                SubstituteBindings::class,
                DisableBladeIconComponents::class,
                DispatchServingFilamentEvent::class,
            ])

            // Authentication
            ->authMiddleware([
                Authenticate::class,
                \App\Http\Middleware\EnsureUserIsUser::class,
            ])

            // Notifications
            ->databaseNotifications()
            ->databaseNotificationsPolling('1s');
    }
}
