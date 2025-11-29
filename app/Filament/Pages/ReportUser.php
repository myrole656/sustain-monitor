<?php

namespace App\Filament\Pages;

use Filament\Pages\Page;
use App\Filament\User\Widgets\ProjectDropDown;
use App\Filament\User\Widgets\UserProjectStats;
use App\Filament\User\Widgets\ResultChart;
use App\Filament\User\Widgets\TargetProject;
use App\Filament\User\Widgets\SDG;
use App\Filament\User\Widgets\OpenPdfWidget;
use BackedEnum;

class ReportUser extends Page
{
    protected string $view = 'filament.pages.report-user';
    protected static BackedEnum|string|null $navigationIcon = 'heroicon-o-users';

    // FIXED signature
    public function getColumns(): array | int
    {
        return 12;
    }

    protected function getHeaderWidgets(): array
    {
        return [
            ProjectDropDown::class,
            OpenPdfWidget::class,
            UserProjectStats::class,
            ResultChart::class,
            TargetProject::class,
            SDG::class,
        ];
    }

    // Correct header widget span function
    public function getHeaderWidgetsColumnSpan(): array
    {
        return [
            ProjectDropDown::class => 6,
            OpenPdfWidget::class => 6,
        ];
    }
}
