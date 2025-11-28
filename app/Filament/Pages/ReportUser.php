<?php

namespace App\Filament\Pages;

use Filament\Pages\Page;
use App\Filament\User\Widgets\ProjectDropDown;
use App\Filament\Widgets\UserInfo;
use App\Filament\User\Widgets\UserProjectStats;
use App\Filament\User\Widgets\ResultChart;
use App\Filament\User\Widgets\TargetProject;
use App\Filament\User\Widgets\SDG;


class ReportUser extends Page
{
    protected string $view = 'filament.pages.report-user';


     protected function getHeaderWidgets(): array
    {
        return [
            ProjectDropDown::class,
      
            UserProjectStats::class,
            ResultChart::class,
            TargetProject::class,
            SDG::class,
        ];
    }
}
