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
}
