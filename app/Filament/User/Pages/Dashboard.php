<?php

namespace App\Filament\User\Pages;

use Filament\Pages\Dashboard as BaseDashboard;
use App\Filament\User\Widgets\ResultChart;
use App\Filament\User\Widgets\ProjectDropDown;
use App\Filament\User\Widgets\UserProjectStats;
use App\Filament\User\Widgets\TargetProject;
use App\Filament\User\Widgets\OpenPdfWidget;

class Dashboard extends BaseDashboard
{
    protected static ?string $title = 'Dashboard';

    // FIXED signature
    public function getColumns(): array | int
    {
        return 12;
    }

    public function getWidgets(): array
    {
        return [
            ProjectDropDown::class,
            OpenPdfWidget::class,
            UserProjectStats::class,
            ResultChart::class,
            TargetProject::class,
        ];
    }

    // Column spans for dashboard widgets
    public function getWidgetColumnSpan(): array
    {
        return [
            ProjectDropDown::class => 6,
            OpenPdfWidget::class => 6,
        ];
    }
}
