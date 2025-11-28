<?php

namespace App\Filament\Pages;

use Filament\Pages\Dashboard as BaseDashboard;
use App\Filament\Widgets\SdgChart;
use App\Filament\Widgets\BlogPostsChart;

class Dashboard extends BaseDashboard
{
    protected static ?string $title = 'Dashboard';

    public function getWidgets(): array
    {
        return [
            SdgChart::class, // Will be at top
           
        ];
    }
}
