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

    // Enable 12-column grid
    public function getColumns(): array | int | string
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

    // Assign column span for widgets
    public function getWidgetColumnSpan(): array
    {
        return [
            ProjectDropDown::class => 6,   // left side
            OpenPdfWidget::class => 6,     // right side
        ];
    }
}
