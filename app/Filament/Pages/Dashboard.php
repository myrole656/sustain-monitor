<?php

namespace App\Filament\Pages;

use Filament\Pages\Dashboard as BaseDashboard;
use App\Filament\Widgets\BlogPostsChart;

class Dashboard extends BaseDashboard
{
    protected static ?string $title = 'Dashboard';

    // Add widgets to the dashboard
    public function getWidgets(): array
    {
        return [
            BlogPostsChart::class,
        ];
    }

}
